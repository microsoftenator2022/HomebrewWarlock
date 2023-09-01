using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Localization;

using ModMenu.Settings;

using UniRx;

namespace HomebrewWarlock
{
    internal class Settings
    {
        public static readonly Settings Instance = new();

        private static string SettingsRootKey => Main.Instance.ModEntry?.Info?.Id?.ToLower()!;
        private static string SettingKey(string key) => SettingsRootKey is null ? null! : $"{SettingsRootKey}.{key}".ToLower();

        private static T GetValue<T>(string key)
        {
            T value = default!;
            
            try
            {
                value = ModMenu.ModMenu.GetSettingValue<T>(key);
            }
            catch (NullReferenceException nre)
            {
                MicroLogger.Warning($"Null Reference Exception retrieving value for setting {key}", nre);
            }

            return value;
        }

        public interface ISetting
        {
            string Key { get; }
            Type Type { get; }
        }

        public class Setting<T> : ISetting, IDisposable
        {
            public Setting(string key, IObservable<T> changed, T defaultValue = default!)
            {
                Key = key;
                Changed = changed;
                value = defaultValue;

                updateValue = Changed.Subscribe(newValue => this.value = newValue);
            }

            public string Key { get; }
            Type ISetting.Type => typeof(T);

            private readonly IDisposable updateValue;
            private T value;
            public T Value => value;

            public IObservable<T> Changed { get; }

            void IDisposable.Dispose() => updateValue.Dispose();
        }

        public IObservable<ISetting> SettingChanged { get; private set; } = Observable.Empty<ISetting>();

        public readonly List<SettingsGroup> Groups = new();
        
        public record class SettingsGroup(Func<SettingsBuilder, SettingsBuilder> Build, string? Name = null)
        {
            public SettingsGroup(string? Name = null) : this(Functional.Identity, Name) { }

            public IObservable<ISetting> SettingChanged { get; init; } = Observable.Empty<ISetting>();
            private readonly Subject<Unit> ForcedUpdate = new();
            public void ForceUpdate() => ForcedUpdate.OnNext(Unit.Default);

            private SettingsGroup AddSetting(
                Func<SettingsBuilder, SettingsBuilder> addSetting,
                IObservable<ISetting>? changed = null)
            {
                return this with
                {
                    Build = sb => addSetting(this.Build(sb)),
                    SettingChanged = changed is not null ? this.SettingChanged.Merge(changed) : this.SettingChanged
                };
            }

            public (SettingsGroup, Setting<bool>) AddToggle(
                string name,
                LocalizedString description,
                bool defaultValue = true,
                LocalizedString? longDescription = null,
                IObserver<bool>? onChange = null)
            {
                var key = SettingKey(name);

                MicroLogger.Debug(sb =>
                {
                    if (this.Name is not null)
                    {
                        sb.AppendLine($"Settings Group: {this.Name}");
                        sb.Append("  ");
                    }

                    sb.Append($"New toggle: key = \"{key}\"");
                });

                var toggle = Toggle.New(key, defaultValue, description);

                if (longDescription is not null)
                    toggle = toggle.WithLongDescription(longDescription);

                var subject = new Subject<(ISetting setting, bool value)>();

                var setting = new Setting<bool>(key, subject.Select(s => s.value), defaultValue);

                toggle = toggle
                    .OnValueChanged(value => subject.OnNext((setting, value)));

                if (onChange is not null) setting.Changed.Subscribe(onChange);

                ForcedUpdate
                    .Select(_ => (setting as ISetting, Settings.GetValue<bool>(key)))
                    .Subscribe(subject);

                return (this.AddSetting(sb => sb.AddToggle(toggle), subject.Select(s => s.setting)), setting);
            }

            public (SettingsGroup, Setting<TListItem>) AddDropdown<TListItem>(
                string name,
                LocalizedString description,
                List<(TListItem item, LocalizedString text)> items,
                int defaultIndex = 0,
                LocalizedString? longDescription = null,
                IObserver<TListItem>? onChange = null)
            {
                var key = SettingKey(name);

                MicroLogger.Debug(sb =>
                {
                    if (this.Name is not null)
                    {
                        sb.AppendLine($"Settings Group: {this.Name}");
                        sb.Append("  ");
                    }

                    sb.Append($"New dropdown list: key = \"{key}\"");
                });

                var list = new DropdownList(key, defaultIndex, description, items.Select(item => item.text).ToList());

                if (longDescription is not null)
                    list = list.WithLongDescription(longDescription);

                var subject = new Subject<(ISetting setting, TListItem value)>();

                var setting = new Setting<TListItem>(key, subject.Select(s => s.value), items[defaultIndex].item);

                list = list.OnValueChanged(value => subject.OnNext((setting, items[value].item)));

                if (onChange is not null) setting.Changed.Subscribe(onChange);

                this.ForcedUpdate
                    .Select(_ => (setting as ISetting, items[Settings.GetValue<int>(key)].item))
                    .Subscribe(subject);

                return (this.AddSetting(sb => sb.AddDropdownList(list), subject.Select(s => s.setting)), setting);
            }

            public SettingsGroup AddSubHeader(LocalizedString title, bool startExpanded = false) =>
                this.AddSetting(sb => sb.AddSubHeader(title, startExpanded));
        }
        
        public void AddSettings(LocalizedString title)
        {
            if (!Groups.Any()) return;

            if (SettingsRootKey is null)
            {
                MicroLogger.Critical("Unable to get root settings key");
                return;
            }

            SettingChanged = Groups.Select(g => g.SettingChanged).Merge();

            var builder = SettingsBuilder.New(SettingsRootKey, title);

            builder = Groups.Aggregate(builder, (acc, group) => group.Build(acc));

            ModMenu.ModMenu.AddSettings(builder);

            foreach (var group in Groups)
            {
                group.ForceUpdate();
            }
        }

        [LocalizedString]
        internal const string Title = "Warlock Homebrew";

        [Init]
        internal static void Init() =>
            Triggers.BlueprintsCache_Init_Early
                .Subscribe(_ => Settings.Instance.AddSettings(LocalizedStrings.Settings_Title));

        // Load localization early. Otherwise some setting strings will be missing
        [Init]
        internal static void InitLocalization() =>
            Triggers.LocalizationManager_Init_Postfix
                .Where(_ => LocalizationManager.CurrentPack is not null)
                .Take(1)
                .Subscribe(_ => LocalizationManager.CurrentPack.AddStrings(LocalizedStrings.GetLocalizationPack(LocalizationManager.CurrentLocale)));
    }
}

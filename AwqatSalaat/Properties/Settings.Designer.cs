﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AwqatSalaat.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.9.0.0")]
    public sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsConfigured {
            get {
                return ((bool)(this["IsConfigured"]));
            }
            set {
                this["IsConfigured"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string CountryCode {
            get {
                return ((string)(this["CountryCode"]));
            }
            set {
                this["CountryCode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string ZipCode {
            get {
                return ((string)(this["ZipCode"]));
            }
            set {
                this["ZipCode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MWL")]
        public global::AwqatSalaat.Services.IslamicFinder.IslamicFinderMethod Method {
            get {
                return ((global::AwqatSalaat.Services.IslamicFinder.IslamicFinderMethod)(this["Method"]));
            }
            set {
                this["Method"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool ShowCountdown {
            get {
                return ((bool)(this["ShowCountdown"]));
            }
            set {
                this["ShowCountdown"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public ushort NotificationDistance {
            get {
                return ((ushort)(this["NotificationDistance"]));
            }
            set {
                this["NotificationDistance"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string ApiCache {
            get {
                return ((string)(this["ApiCache"]));
            }
            set {
                this["ApiCache"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string DisplayLanguage {
            get {
                return ((string)(this["DisplayLanguage"]));
            }
            set {
                this["DisplayLanguage"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string City {
            get {
                return ((string)(this["City"]));
            }
            set {
                this["City"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MWL")]
        public global::AwqatSalaat.Services.AlAdhan.AlAdhanMethod Method2 {
            get {
                return ((global::AwqatSalaat.Services.AlAdhan.AlAdhanMethod)(this["Method2"]));
            }
            set {
                this["Method2"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("IslamicFinder")]
        public global::AwqatSalaat.Data.PrayerTimesService Service {
            get {
                return ((global::AwqatSalaat.Data.PrayerTimesService)(this["Service"]));
            }
            set {
                this["Service"] = value;
            }
        }
    }
}

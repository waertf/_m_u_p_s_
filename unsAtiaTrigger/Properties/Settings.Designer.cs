﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace unsAtiaTrigger.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "11.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10.53.1.114")]
        public string ReceiveAtiaMsgIp {
            get {
                return ((string)(this["ReceiveAtiaMsgIp"]));
            }
            set {
                this["ReceiveAtiaMsgIp"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10.53.1.xxx")]
        public string BlockAtiaMsgIp {
            get {
                return ((string)(this["BlockAtiaMsgIp"]));
            }
            set {
                this["BlockAtiaMsgIp"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[00000007] Realtek PCIe GBE Family Controller")]
        public string ATIA_NIC_NAME {
            get {
                return ((string)(this["ATIA_NIC_NAME"]));
            }
            set {
                this["ATIA_NIC_NAME"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10.6.3.17")]
        public string NorthIpAddress {
            get {
                return ((string)(this["NorthIpAddress"]));
            }
            set {
                this["NorthIpAddress"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("5555")]
        public string RemotePort {
            get {
                return ((string)(this["RemotePort"]));
            }
            set {
                this["RemotePort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Atia")]
        public string AtiaServiceName {
            get {
                return ((string)(this["AtiaServiceName"]));
            }
            set {
                this["AtiaServiceName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\CGAWork\\Atia")]
        public string AtiaProcessPath {
            get {
                return ((string)(this["AtiaProcessPath"]));
            }
            set {
                this["AtiaProcessPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Uns")]
        public string UnsServiceName {
            get {
                return ((string)(this["UnsServiceName"]));
            }
            set {
                this["UnsServiceName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\CGAWork\\Uns")]
        public string UnsProcessPath {
            get {
                return ((string)(this["UnsProcessPath"]));
            }
            set {
                this["UnsProcessPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Process")]
        public string AtiaUnsServiceOrProcess {
            get {
                return ((string)(this["AtiaUnsServiceOrProcess"]));
            }
            set {
                this["AtiaUnsServiceOrProcess"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("ATIA_2.exe")]
        public string AtiaProcessName {
            get {
                return ((string)(this["AtiaProcessName"]));
            }
            set {
                this["AtiaProcessName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Client.exe")]
        public string UnsProcessName {
            get {
                return ((string)(this["UnsProcessName"]));
            }
            set {
                this["UnsProcessName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[00000007] Realtek PCIe GBE Family Controller")]
        public string UNS_NIC_NAME {
            get {
                return ((string)(this["UNS_NIC_NAME"]));
            }
            set {
                this["UNS_NIC_NAME"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10.6.3.16")]
        public string ReceiveUnsMsgIp {
            get {
                return ((string)(this["ReceiveUnsMsgIp"]));
            }
            set {
                this["ReceiveUnsMsgIp"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10.6.3.12")]
        public string BlockUnsMsgIp {
            get {
                return ((string)(this["BlockUnsMsgIp"]));
            }
            set {
                this["BlockUnsMsgIp"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("netsh interface ip add address \\\"SLOT 2 連接埠 1\\\" 10.53.1.114  255.255.255.0")]
        public string AddAtiaIpAdressAndSubnetInSouthServer {
            get {
                return ((string)(this["AddAtiaIpAdressAndSubnetInSouthServer"]));
            }
            set {
                this["AddAtiaIpAdressAndSubnetInSouthServer"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("netsh interface ip delete address \\\"SLOT 2 連接埠 1\\\" 10.53.1.114")]
        public string RemoveAtiaIpAddressFromSouthServer {
            get {
                return ((string)(this["RemoveAtiaIpAddressFromSouthServer"]));
            }
            set {
                this["RemoveAtiaIpAddressFromSouthServer"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("netsh interface ip add address \\\"NIC2\\\" 10.53.1.114  255.255.255.0")]
        public string AddAtiaIpAdressAndSubnetInNorthServer {
            get {
                return ((string)(this["AddAtiaIpAdressAndSubnetInNorthServer"]));
            }
            set {
                this["AddAtiaIpAdressAndSubnetInNorthServer"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("netsh interface ip delete address \\\"NIC2\\\" 10.53.1.114")]
        public string RemoveAtiaIpAddressFromNorthServer {
            get {
                return ((string)(this["RemoveAtiaIpAddressFromNorthServer"]));
            }
            set {
                this["RemoveAtiaIpAddressFromNorthServer"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("South")]
        public string Location {
            get {
                return ((string)(this["Location"]));
            }
            set {
                this["Location"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10.6.3.103")]
        public string SouthIpAddress {
            get {
                return ((string)(this["SouthIpAddress"]));
            }
            set {
                this["SouthIpAddress"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("ProcessMonitor.exe")]
        public string unsMonitorName {
            get {
                return ((string)(this["unsMonitorName"]));
            }
            set {
                this["unsMonitorName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("ProcessMonitor.exe")]
        public string atiaMonitorName {
            get {
                return ((string)(this["atiaMonitorName"]));
            }
            set {
                this["atiaMonitorName"] = value;
            }
        }
    }
}

﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CLRFunctionPerformanceCounter.SandboxDatabaseManagerService {
    using System.Runtime.Serialization;
    using System;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="PerformanceCounterResults", Namespace="http://schemas.datacontract.org/2004/07/SandboxDatabaseManagerService")]
    [System.SerializableAttribute()]
    public partial class PerformanceCounterResults : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private CLRFunctionPerformanceCounter.SandboxDatabaseManagerService.PerformanceCounterResults.PerformanceCounterResult[] ResultListField;
        
        [global::System.ComponentModel.BrowsableAttribute(false)]
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public CLRFunctionPerformanceCounter.SandboxDatabaseManagerService.PerformanceCounterResults.PerformanceCounterResult[] ResultList {
            get {
                return this.ResultListField;
            }
            set {
                if ((object.ReferenceEquals(this.ResultListField, value) != true)) {
                    this.ResultListField = value;
                    this.RaisePropertyChanged("ResultList");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
        
        [System.Diagnostics.DebuggerStepThroughAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
        [System.Runtime.Serialization.DataContractAttribute(Name="PerformanceCounterResults.PerformanceCounterResult", Namespace="http://schemas.datacontract.org/2004/07/SandboxDatabaseManagerService")]
        [System.SerializableAttribute()]
        public partial class PerformanceCounterResult : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
            
            [System.NonSerializedAttribute()]
            private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
            
            [System.Runtime.Serialization.OptionalFieldAttribute()]
            private string CounterFormattedValueField;
            
            [System.Runtime.Serialization.OptionalFieldAttribute()]
            private string CounterFriendlyNameField;
            
            public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
                get {
                    return this.extensionDataField;
                }
                set {
                    this.extensionDataField = value;
                }
            }
            
            [System.Runtime.Serialization.DataMemberAttribute()]
            public string CounterFormattedValue {
                get {
                    return this.CounterFormattedValueField;
                }
                set {
                    if ((object.ReferenceEquals(this.CounterFormattedValueField, value) != true)) {
                        this.CounterFormattedValueField = value;
                        this.RaisePropertyChanged("CounterFormattedValue");
                    }
                }
            }
            
            [System.Runtime.Serialization.DataMemberAttribute()]
            public string CounterFriendlyName {
                get {
                    return this.CounterFriendlyNameField;
                }
                set {
                    if ((object.ReferenceEquals(this.CounterFriendlyNameField, value) != true)) {
                        this.CounterFriendlyNameField = value;
                        this.RaisePropertyChanged("CounterFriendlyName");
                    }
                }
            }
            
            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
            
            protected void RaisePropertyChanged(string propertyName) {
                System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
                if ((propertyChanged != null)) {
                    propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
                }
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="SandboxDatabaseManagerService.ISandboxDatabaseManagerService")]
    public interface ISandboxDatabaseManagerService {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISandboxDatabaseManagerService/GetPerformanceCounterResult", ReplyAction="http://tempuri.org/ISandboxDatabaseManagerService/GetPerformanceCounterResultResponse")]
        CLRFunctionPerformanceCounter.SandboxDatabaseManagerService.PerformanceCounterResults GetPerformanceCounterResult();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ISandboxDatabaseManagerService/GetPerformanceCounterResult", ReplyAction="http://tempuri.org/ISandboxDatabaseManagerService/GetPerformanceCounterResultResponse")]
        System.Threading.Tasks.Task<CLRFunctionPerformanceCounter.SandboxDatabaseManagerService.PerformanceCounterResults> GetPerformanceCounterResultAsync();
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface ISandboxDatabaseManagerServiceChannel : CLRFunctionPerformanceCounter.SandboxDatabaseManagerService.ISandboxDatabaseManagerService, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class SandboxDatabaseManagerServiceClient : System.ServiceModel.ClientBase<CLRFunctionPerformanceCounter.SandboxDatabaseManagerService.ISandboxDatabaseManagerService>, CLRFunctionPerformanceCounter.SandboxDatabaseManagerService.ISandboxDatabaseManagerService {
        
        public SandboxDatabaseManagerServiceClient() {
        }
        
        public SandboxDatabaseManagerServiceClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public SandboxDatabaseManagerServiceClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public SandboxDatabaseManagerServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public SandboxDatabaseManagerServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public CLRFunctionPerformanceCounter.SandboxDatabaseManagerService.PerformanceCounterResults GetPerformanceCounterResult() {
            return base.Channel.GetPerformanceCounterResult();
        }
        
        public System.Threading.Tasks.Task<CLRFunctionPerformanceCounter.SandboxDatabaseManagerService.PerformanceCounterResults> GetPerformanceCounterResultAsync() {
            return base.Channel.GetPerformanceCounterResultAsync();
        }
    }
}

﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.DotNet.Scaffolding.Shared {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class MessageStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal MessageStrings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.DotNet.Scaffolding.Shared.MessageStrings", typeof(MessageStrings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Add directory: &apos;{0}&apos;.
        /// </summary>
        internal static string AddDirectoryMessage {
            get {
                return ResourceManager.GetString("AddDirectoryMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Add File: &apos;{0}&apos;.
        /// </summary>
        internal static string AddFileMessage {
            get {
                return ResourceManager.GetString("AddFileMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Contents: {0}.
        /// </summary>
        internal static string ContentsMessage {
            get {
                return ResourceManager.GetString("ContentsMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Deleted file: &apos;{0}&apos;.
        /// </summary>
        internal static string DeleteFileMessage {
            get {
                return ResourceManager.GetString("DeleteFileMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Edit File: &apos;{0}&apos;.
        /// </summary>
        internal static string EditFileMessage {
            get {
                return ResourceManager.GetString("EditFileMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to :::End FileSystemChange:::.
        /// </summary>
        internal static string EndFileSystemChangeToken {
            get {
                return ResourceManager.GetString("EndFileSystemChangeToken", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid FileSystemChange message..
        /// </summary>
        internal static string InvalidFileSystemChangeMessage {
            get {
                return ResourceManager.GetString("InvalidFileSystemChangeMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ProjectInformation Response received is not valid..
        /// </summary>
        internal static string InvalidProjectInformationMessage {
            get {
                return ResourceManager.GetString("InvalidProjectInformationMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The protocol version &apos;{0}&apos; of the message is different than currently handled version &apos;{1}&apos;..
        /// </summary>
        internal static string ProtocolVersionMismatch {
            get {
                return ResourceManager.GetString("ProtocolVersionMismatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Deleted directory: &apos;{0}&apos;.
        /// </summary>
        internal static string RemoveDirectoryMessage {
            get {
                return ResourceManager.GetString("RemoveDirectoryMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to :::Start FileSystemChange:::.
        /// </summary>
        internal static string StartFileSystemChangeToken {
            get {
                return ResourceManager.GetString("StartFileSystemChangeToken", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Project path has not been specified.
        /// </summary>
        internal static string ProjectPathNotGiven {
            get {
                return ResourceManager.GetString("ProjectPathNotGiven", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Target location (the build directory) has not been specified.
        /// </summary>
        internal static string TargetLocationNotGiven {
            get {
                return ResourceManager.GetString("TargetLocationNotGiven", resourceCulture);
            }
        }
      }
}

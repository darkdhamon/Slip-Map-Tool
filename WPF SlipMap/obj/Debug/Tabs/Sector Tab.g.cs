﻿#pragma checksum "..\..\..\Tabs\Sector Tab.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "A97F2D431DEFA38B7922261D8D93ED91A3DAAFFA"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using WPF_SlipMap.Tabs;


namespace WPF_SlipMap.Tabs {
    
    
    /// <summary>
    /// SectorTab
    /// </summary>
    public partial class SectorTab : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 18 "..\..\..\Tabs\Sector Tab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox SectorName;
        
        #line default
        #line hidden
        
        
        #line 23 "..\..\..\Tabs\Sector Tab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button SaveSector;
        
        #line default
        #line hidden
        
        
        #line 32 "..\..\..\Tabs\Sector Tab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox Sectors;
        
        #line default
        #line hidden
        
        
        #line 33 "..\..\..\Tabs\Sector Tab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button LoadSector;
        
        #line default
        #line hidden
        
        
        #line 51 "..\..\..\Tabs\Sector Tab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox CreateSectorName;
        
        #line default
        #line hidden
        
        
        #line 53 "..\..\..\Tabs\Sector Tab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox CreateLastID;
        
        #line default
        #line hidden
        
        
        #line 56 "..\..\..\Tabs\Sector Tab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox RandomSystem;
        
        #line default
        #line hidden
        
        
        #line 57 "..\..\..\Tabs\Sector Tab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid SetSystem;
        
        #line default
        #line hidden
        
        
        #line 63 "..\..\..\Tabs\Sector Tab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox CreateStartID;
        
        #line default
        #line hidden
        
        
        #line 67 "..\..\..\Tabs\Sector Tab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button CreateSector;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/WPF SlipMap;component/tabs/sector%20tab.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\Tabs\Sector Tab.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 12 "..\..\..\Tabs\Sector Tab.xaml"
            ((System.Windows.Controls.Expander)(target)).Expanded += new System.Windows.RoutedEventHandler(this.Expander_OnExpanded);
            
            #line default
            #line hidden
            return;
            case 2:
            this.SectorName = ((System.Windows.Controls.TextBox)(target));
            return;
            case 3:
            this.SaveSector = ((System.Windows.Controls.Button)(target));
            
            #line 23 "..\..\..\Tabs\Sector Tab.xaml"
            this.SaveSector.Click += new System.Windows.RoutedEventHandler(this.SaveSector_OnClick);
            
            #line default
            #line hidden
            return;
            case 4:
            
            #line 26 "..\..\..\Tabs\Sector Tab.xaml"
            ((System.Windows.Controls.Expander)(target)).Expanded += new System.Windows.RoutedEventHandler(this.Expander_OnExpanded);
            
            #line default
            #line hidden
            return;
            case 5:
            this.Sectors = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 6:
            this.LoadSector = ((System.Windows.Controls.Button)(target));
            
            #line 33 "..\..\..\Tabs\Sector Tab.xaml"
            this.LoadSector.Click += new System.Windows.RoutedEventHandler(this.LoadSector_OnClick);
            
            #line default
            #line hidden
            return;
            case 7:
            this.CreateSectorName = ((System.Windows.Controls.TextBox)(target));
            return;
            case 8:
            this.CreateLastID = ((System.Windows.Controls.TextBox)(target));
            return;
            case 9:
            this.RandomSystem = ((System.Windows.Controls.CheckBox)(target));
            
            #line 56 "..\..\..\Tabs\Sector Tab.xaml"
            this.RandomSystem.Unchecked += new System.Windows.RoutedEventHandler(this.RandomSystem_OnUnchecked);
            
            #line default
            #line hidden
            
            #line 56 "..\..\..\Tabs\Sector Tab.xaml"
            this.RandomSystem.Checked += new System.Windows.RoutedEventHandler(this.RandomSystem_Checked);
            
            #line default
            #line hidden
            return;
            case 10:
            this.SetSystem = ((System.Windows.Controls.Grid)(target));
            return;
            case 11:
            this.CreateStartID = ((System.Windows.Controls.TextBox)(target));
            return;
            case 12:
            this.CreateSector = ((System.Windows.Controls.Button)(target));
            
            #line 68 "..\..\..\Tabs\Sector Tab.xaml"
            this.CreateSector.Click += new System.Windows.RoutedEventHandler(this.CreateSector_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}


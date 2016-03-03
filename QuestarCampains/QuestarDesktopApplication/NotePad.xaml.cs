// QuestarCampains QuestarDesktopApplication NotePad.xaml.cs
// Created: 2016-03-03 12:07 PM
// Last Edited: 2016-03-03 12:10 PM
// 
// Author: Bronze Harold Brown

#region Imports

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

#endregion

namespace QuestarDesktopApplication
{
   /// <summary>
   ///    Interaction logic for NotePad.xaml
   /// </summary>
   public partial class NotePad
   {
      public NotePad()
      {
         InitializeComponent();

         CmbFontFamily.ItemsSource = Fonts.SystemFontFamilies.OrderBy(f => f.Source);
         CmbFontSize.ItemsSource = new List<double> {8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72};
      }

      private void rtbEditor_SelectionChanged(object sender, RoutedEventArgs e)
      {
         var temp = RtbEditor.Selection.GetPropertyValue(TextElement.FontWeightProperty);
         BtnBold.IsChecked = (temp != DependencyProperty.UnsetValue) && temp.Equals(FontWeights.Bold);
         temp = RtbEditor.Selection.GetPropertyValue(TextElement.FontStyleProperty);
         BtnItalic.IsChecked = (temp != DependencyProperty.UnsetValue) && temp.Equals(FontStyles.Italic);
         temp = RtbEditor.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
         BtnUnderline.IsChecked = (temp != DependencyProperty.UnsetValue) && temp.Equals(TextDecorations.Underline);

         temp = RtbEditor.Selection.GetPropertyValue(TextElement.FontFamilyProperty);
         CmbFontFamily.SelectedItem = temp;
         temp = RtbEditor.Selection.GetPropertyValue(TextElement.FontSizeProperty);
         CmbFontSize.Text = temp.ToString();
      }

      private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
      {
         var dlg = new OpenFileDialog {Filter = "Rich Text Format (*.rtf)|*.rtf|All files (*.*)|*.*"};
         if (dlg.ShowDialog() == true)
         {
            var fileStream = new FileStream(dlg.FileName, FileMode.Open);
            var range = new TextRange(RtbEditor.Document.ContentStart, RtbEditor.Document.ContentEnd);
            range.Load(fileStream, DataFormats.Rtf);
         }
      }

      private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
      {
         var dlg = new SaveFileDialog {Filter = "Rich Text Format (*.rtf)|*.rtf|All files (*.*)|*.*"};
         if (dlg.ShowDialog() != true) return;
         var fileStream = new FileStream(dlg.FileName, FileMode.Create);
         var range = new TextRange(RtbEditor.Document.ContentStart, RtbEditor.Document.ContentEnd);
         range.Save(fileStream, DataFormats.Rtf);
      }

      private void cmbFontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (CmbFontFamily.SelectedItem != null)
            RtbEditor.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, CmbFontFamily.SelectedItem);
      }

      private void cmbFontSize_TextChanged(object sender, TextChangedEventArgs e)
      {
         RtbEditor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, CmbFontSize.Text);
      }
   }
}
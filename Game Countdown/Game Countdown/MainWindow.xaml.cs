// Game Countdown Game Countdown MainWindow.xaml.cs
// Created: 2016-02-26 12:53 PM
// Last Edited: 2016-02-26 1:06 PM
// 
// Author: Bronze Harold Brown

#region Imports

using System;
using System.Threading;
using System.Windows;

#endregion

namespace Game_Countdown
{
  /// <summary>
  ///   Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private readonly DateTime timeOfGame;

    public MainWindow()
    {
      InitializeComponent();
      timeOfGame = new DateTime(2016, 2, 26, 21, 0, 0);
      var time = new Timer(UpdateCountdown, new object(), 0, 1000);
    }

    private void UpdateCountdown(object blah)
    {
      var currentTime = DateTime.Now;
      var span = timeOfGame - currentTime;
      Dispatcher.Invoke(
        () =>
        {
          Countdown.Content = $"{span.Days} days, {span.Hours} hours, {span.Minutes} minutes, {span.Seconds} seconds";
        });

    }
  }
}
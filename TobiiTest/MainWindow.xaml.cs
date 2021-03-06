﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tobii.Interaction;
using System.Windows.Forms.Integration;
using System.Runtime.InteropServices;

namespace TobiiTest
{
    
    public partial class MainWindow : Window
    {
        public Preferences pref;
        Gazer gazer;
        Translator translator;

        string SourceLanguage
        {
            get;
            set;
        }
        string TargetLanguage
        {
            get;
            set;
        }

        public MainWindow()
        {
            InitializeComponent();

            pref = new Preferences();
            gazer = new Gazer();

            UpdateSourceCB();
            UpdateTargCB();

            sourceLanguageCB.SelectionChanged += SourceLanguageCB_SelectionChanged;
            targetLanguageCB.SelectionChanged += TargetLanguageCB_SelectionChanged;
        }

        // Fill source language combo box
        private void UpdateSourceCB()
        {
            sourceLanguageCB.Items.Clear();
            
            sourceLanguageCB.Items.Add(new ComboBoxItem()
            {
                Content = "Select Source Language",
                Visibility = Visibility.Collapsed
            });
            sourceLanguageCB.SelectedIndex = 0;
            
            foreach (string lang in OCRUtil.AvailableOCRLangs().Values)
            {
                sourceLanguageCB.Items.Add(lang);
            }
            targetLanguageCB.SelectedIndex = 0;
        }

        // Fill target language combo box
        private void UpdateTargCB()
        {
            targetLanguageCB.Items.Clear();
            
            targetLanguageCB.Items.Add(new ComboBoxItem()
            {
                Content = "Select Target Language",
                Visibility = Visibility.Collapsed
            });
            targetLanguageCB.SelectedIndex = 0;
            
            switch (pref.Get("translator"))
            {
                case "Google":
                    foreach (string lang in GoogleTranslator.Languages)
                    {
                        targetLanguageCB.Items.Add(lang);
                    }
                    break;
                case "Microsoft":
                    foreach (string lang in MicrosoftTranslator.Languages.Keys)
                    {
                        targetLanguageCB.Items.Add(lang);
                    }
                    break;
                case "Yandex":
                    foreach (string lang in YandexTranslator.Languages.Keys)
                    {
                        targetLanguageCB.Items.Add(lang);
                    }
                    break;
                default:
                    throw new ArgumentException("Unknown translator: " + pref.Get("translator"));
            }
            targetLanguageCB.SelectedIndex = 0;
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            var oldTr = pref.Get("translator");
            var pr = new Preference(pref);
            Console.WriteLine(pr.prefs);
            pr.ShowDialog();
            if (!oldTr.Equals(pref.Get("translator")))
            {
                Console.WriteLine("MW: translator changed");
                UpdateTargCB();
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            new About().ShowDialog();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            new Help().ShowDialog();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.ToString().Equals(Enum.Parse(typeof(Key), pref.Get("key")).ToString()))
            {
                if (!gazer.State)
                {
                    gazer.Start();
                }
                else
                {
                    var coords = gazer.Stop();
                    // Get screenshot size
                    string ssize = pref.Get("sssize");
                    Tuple<int, int> size;
                    switch (ssize)
                    {
                        case "Small":
                            size = Tuple.Create(200, 200);
                            break;
                        case "Medium":
                            size = Tuple.Create(400, 400);
                            break;
                        case "Large":
                            size = Tuple.Create(600, 600);
                            break;
                        case "Custom":
                            size = Tuple.Create(Int32.Parse(pref.Get("screenx")), Int32.Parse(pref.Get("screeny")));
                            break;
                        default:
                            throw new ArgumentException("No such screenshot size: " + ssize);
                    }
                    var screen = ScreenshotUtil.TakeScreen(coords.Item1, coords.Item2, size);
                    var text = OCRUtil.RecognizeImage(screen);
                    srcTextTB.Text = text;
                    
                    translator = Translator.Create(pref.Get("translator"), SourceLanguage, TargetLanguage);
                    try
                    {
                        var tl = translator.Translate(text);
                        if (translator is GoogleTranslator)
                        {
                            var gt = (GoogleTranslator)translator;
                            if (gt.Error != null)
                            {
                                MessageBox.Show("Google translate error.\nMessage: " + gt.Error.Message + "\nStack trace: " + gt.Error.StackTrace, "GT Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        } 
                        Console.WriteLine("MW: translated text: " + tl);
                        targTextTB.Text = tl;
                    }
                    catch (Newtonsoft.Json.JsonReaderException ex)
                    {
                        MessageBox.Show("Error while parsing json: " + ex.Message + "\n", "JSON Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void SourceLanguageCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!targetLanguageCB.SelectedItem.ToString().Equals("Select Target Language"))
            {
                SourceLanguage = sourceLanguageCB.SelectedItem.ToString();
            }
                
        }

        private void TargetLanguageCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (targetLanguageCB.SelectedItem != null && !targetLanguageCB.SelectedItem.ToString().Equals("Select Target Language"))
            {
                TargetLanguage = (string)targetLanguageCB.SelectedItem.ToString();
            }
        }
    }
}

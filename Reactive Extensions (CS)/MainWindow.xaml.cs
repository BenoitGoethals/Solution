using Reactive_Extensions__CS_.DictionaryService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Reactive_Extensions__CS_
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SubscribeToTextChanged();
            SubscribeToSelectionChanged();
        }

        private DictServiceSoapClient service = new DictServiceSoapClient("DictServiceSoap");
        private DictServiceSoapClient Service
        {
            get
            {
                return service;
            }
        }

        private IObservable<WordDefinition> DefinitionCall(string word)
        {
            return Observable.FromAsync(() => Service.DefineAsync(word));
        }


        private IObservable<DictionaryWord[]> ServiceCall(string term)
        {
            return Observable.FromAsync(
                                  () => Service.MatchInDictAsync("wn", term, "prefix"));
        }

        private void SubscribeToTextChanged()
        {
            var input = (from e in Observable.FromEventPattern(inputBox, "TextChanged")
                         let s = ((TextBox)e.Sender).Text
                         where s.Length > 1
                         select s)
                         .Throttle(TimeSpan.FromSeconds(0.5)).DistinctUntilChanged();

            Debug.Write("");

            var result = from term in input
                         from w in ServiceCall(term).TakeUntil(input)
                         select w;

            result.ObserveOn(wordsBox).Subscribe(
              (words) => {
                  wordsBox.ItemsSource = words;
                  wordsBox.DisplayMemberPath = "Word";
              },
              (ex) => MessageBox.Show(ex.Message)
            );

        }


        private void SubscribeToSelectionChanged()
        {
            var input = (from e in Observable.FromEventPattern(wordsBox, "SelectionChanged")
                         let w = ((DictionaryWord)((ListBox)e.Sender).SelectedValue)
                         select w).
                         Throttle(TimeSpan.FromSeconds(0.5)).DistinctUntilChanged();

            var res = from term in input
                      where term != null
                      from definition in DefinitionCall(term.Word).TakeUntil(input)
                      select definition;

            res.ObserveOn(resultBox).Subscribe(
              (term) => {
                  resultBox.Text = "";
                  foreach (var definition in term.Definitions)
                  {
                      resultBox.Text += definition.WordDefinition + Environment.NewLine + Environment.NewLine;
                  }
              }
            );
        }

        
    }
}

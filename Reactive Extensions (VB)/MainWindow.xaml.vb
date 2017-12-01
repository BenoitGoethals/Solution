Imports System.Reactive.Linq
Imports System.Data
Imports Reactive_Extensions__VB_.DictionaryService


'  /// <summary>
'  /// Interaction logic for MainWindow.xaml
'  /// </summary>
Class MainWindow
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        SubscribeToTextChanged()
        SubscribeToSelectionChanged()
    End Sub



    Private _service As New DictServiceSoapClient("DictServiceSoap")
    Private ReadOnly Property Service As DictServiceSoapClient
        Get
            Return _service
        End Get

    End Property



    Private Function ServiceCall(term As String) As IObservable(Of DictionaryWord())
        Return Observable.FromAsync(Function() Service.MatchInDictAsync("wn", term, "prefix"))
    End Function

    Private Sub SubscribeToTextChanged()
        Dim input = (From e In Observable.FromEventPattern(inputBox, "TextChanged")
                     Let s = DirectCast(e.Sender, TextBox).Text
                     Where s.Length > 1
                     Select s).Throttle(TimeSpan.FromSeconds(0.5)).DistinctUntilChanged()

        Dim result = From term In input
                     From w In ServiceCall(term).TakeUntil(input)
                     Select w

        result.ObserveOn(wordsBox).Subscribe(
                Sub(words)
                    wordsBox.ItemsSource = words
                    wordsBox.DisplayMemberPath = "Word"
                End Sub,
                Sub(ex) MessageBox.Show(ex.Message)
        )
    End Sub

    Private Function DefinitionCall(word As String) As IObservable(Of WordDefinition)
        Return Observable.FromAsync(Function() Service.DefineAsync(word))
    End Function

    Private Sub SubscribeToSelectionChanged()
        Dim input = (From e In Observable.FromEventPattern(wordsBox, "SelectionChanged")
                     Let w = DirectCast(DirectCast(e.Sender, ListBox).SelectedValue, DictionaryWord)
                     Select w).Throttle(TimeSpan.FromSeconds(0.5)).DistinctUntilChanged()


        Dim res = From term In input
                  Where term IsNot Nothing
                  From definition In DefinitionCall(term.Word).TakeUntil(input)
                  Select definition

        res.ObserveOn(resultBox).Subscribe(
        Sub(term)
            resultBox.Text = ""
            For Each definition In term.Definitions
                resultBox.Text += definition.WordDefinition + Environment.NewLine + Environment.NewLine
            Next
        End Sub
        )

    End Sub
End Class

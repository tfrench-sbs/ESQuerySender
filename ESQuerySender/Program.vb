Imports System
Imports Elastic.Clients.Elasticsearch
Imports Newtonsoft.Json

Module Program
    Sub Main(args As String())
        Console.WriteLine("Type a search term and press Enter, or 'exit' to quit the application.")


        While True
            Console.Write("> ")
            Dim input As String = Console.ReadLine()

            ' Check for exit condition
            If input.ToLower() = "exit" Then
                Console.WriteLine("Exiting...")
                Exit While
            End If

            If String.IsNullOrWhiteSpace(input) Then
                Continue While
            End If

            Try
                Dim searchResults = ElasticSearchInterface.Find(input)

                If searchResults?.Hits IsNot Nothing Then
                    Console.WriteLine($"Total Hits: {searchResults.Hits.Count}")

                    For Each hit In searchResults.Hits
                        Try
                            Dim jsonSettings = New JsonSerializerSettings() With {
                                .Formatting = Formatting.Indented,
                                .Error = Function(sender, eargs)
                                             eargs.ErrorContext.Handled = True
                                             Return Nothing
                                         End Function
                            }

                            Dim jsonString As String = ""

                            Try
                                jsonString = JsonConvert.SerializeObject(hit, jsonSettings)
                            Catch
                                jsonString = JsonConvert.SerializeObject(New With {
                                    .Index = hit.Index,
                                    .Id = hit.Id,
                                    .Score = hit.Score
                                }, jsonSettings)
                            End Try

                            If String.IsNullOrWhiteSpace(jsonString) Then
                                jsonString = $"{{ ""Index"": ""{hit.Index}"", ""Id"": ""{hit.Id}"" }}"
                            End If

                            Console.WriteLine("Hit JSON:")
                            Console.WriteLine(jsonString)
                            Console.WriteLine("-----")

                        Catch
                            Console.WriteLine($"Error processing prod: {hit.Index}, ID: {hit.Id}")
                            Console.WriteLine("-----")
                        End Try
                    Next
                Else
                    Console.WriteLine("No results found.")
                End If
            Catch ex As Exception
            Console.WriteLine($"Error during search: {ex.Message}")
            End Try
        End While
    End Sub



End Module

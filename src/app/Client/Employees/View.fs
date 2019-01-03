module Client.Employees.View

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fulma

open Client
open System.ComponentModel

let employeeList dispatch =
  let employeeLine (employeeNumber: int) =
    let userName = sprintf "employee%d" employeeNumber
    tr [ ]
      [
        td [] [ str userName ]
        td [] [ str "Employee "; str (employeeNumber.ToString()) ]
        td [] [ a [ Href (Pages.toPath (Page.Balance (Some userName))) ] [ str "View balance" ] ]
      ]

  div []
    [
      Table.table [ Table.IsBordered
                    Table.IsStriped ]
        [
          thead []
            [
              yield tr []
                [
                  th [] [str "User"]
                  th [] [str "Employee"]
                  th [] []
                ]
            ]
          tbody []
            [
              for employeeNumber in 1..5 do
                yield employeeLine employeeNumber
            ]
        ]
    ]

let root dispatch =
  div []
    [ Heading.h3 [ ]
        [ str "Employees list" ]
      employeeList dispatch ]

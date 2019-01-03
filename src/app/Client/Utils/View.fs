namespace Client

open Fable.Helpers.React.Props
open Fable.Import

[<AutoOpen>]
module ViewUtils =

  let [<Literal>] ENTER_KEY = 13.

  let OnEnter msg dispatch =
    function 
    | (ev:React.KeyboardEvent) when ev.keyCode = ENTER_KEY ->
        ev.preventDefault()
        dispatch msg
    | _ -> ()
    |> OnKeyDown

module Client.Home.State

open Elmish
open Types

let init () : Model * Cmd<Msg> =
  let counter, cmd = Client.Counter.State.init ()
  { Counter = counter }, Cmd.map CounterMsg cmd

let update msg model : Model * Cmd<Msg> =
  match msg with
  | CounterMsg msg ->
      let counter, cmd = Client.Counter.State.update msg model.Counter
      { model with Counter = counter }, Cmd.map CounterMsg cmd

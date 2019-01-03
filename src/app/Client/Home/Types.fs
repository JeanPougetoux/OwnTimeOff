module Client.Home.Types

type Model = {
  Counter: Client.Counter.Types.Model
}

type Msg =
  | CounterMsg of Client.Counter.Types.Msg

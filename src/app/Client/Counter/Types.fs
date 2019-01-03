module Client.Counter.Types

type Model = {
    Counter: int
}

type Msg =
  | Increment
  | Decrement

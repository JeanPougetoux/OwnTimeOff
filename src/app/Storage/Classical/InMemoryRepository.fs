namespace Storage.Classical

type private InMemoryRepository<'TKey, 'TValue when 'TKey: comparison> () =
  let dictionary = System.Collections.Generic.Dictionary<'TKey, 'TValue>()

  interface IRepository<'TKey, 'TValue> with
    member __.Create (key: 'TKey) (value: 'TValue) = async {
      if dictionary.ContainsKey key then
        return raise KeyExistsException
      else
        dictionary.[key] <- value
        return value }

    member __.Read (key: 'TKey) = async {
      match dictionary.TryGetValue key with
      | true, value -> return Some value
      | _ -> return None }

    member __.ReadAll() = async {
      return dictionary.Values :> seq<_> }

    member __.Update (key: 'TKey) (value: 'TValue) = async {
      if dictionary.ContainsKey key then
        return None
      else
        dictionary.[key] <- value
        return Some value }

    member __.Delete (key: 'TKey) = async {
      dictionary.Remove key |> ignore
      return () }

type InMemoryRepository =
  static member Create<'TKey, 'TValue when 'TKey: comparison>() =
    InMemoryRepository<'TKey, 'TValue>() :> IRepository<'TKey, 'TValue>
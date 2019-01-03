namespace Storage.Classical

open Storage
open System.IO

type private DirectoryRepository<'TKey, 'TValue when 'TKey: comparison> (path: string, keyFormatter: 'TKey -> string) =
  do
    if not (Directory.Exists path) then
      Directory.CreateDirectory(path) |> ignore

  interface IRepository<'TKey, 'TValue> with
    member __.Create (key: 'TKey) (value: 'TValue) = async {
      let filePath = Path.Combine(path, keyFormatter key)
 
      if File.Exists(filePath) then
        return raise KeyExistsException
      else
        do! File.WriteAllTextAsync(filePath, Serialization.serialize value) |> Async.AwaitTask
        return value }

    member __.Read (key: 'TKey) = async {
      let filePath = Path.Combine(path, keyFormatter key)

      if not (File.Exists(filePath)) then
        return None
      else
        let! content = File.ReadAllTextAsync(filePath) |> Async.AwaitTask
        return Some (Serialization.deserialize<'TValue> content) }

    member __.ReadAll() = async {
      let values = ResizeArray<'TValue>()

      for filePath in Directory.GetFiles(path) do
        let! content = File.ReadAllTextAsync(filePath) |> Async.AwaitTask
        values.Add (Serialization.deserialize<'TValue> content)

      return values :> seq<_> }

    member __.Update (key: 'TKey) (value: 'TValue) = async {
      let filePath = Path.Combine(path, keyFormatter key)

      if not (File.Exists(filePath)) then
        return None
      else
        do! File.WriteAllTextAsync(filePath, Serialization.serialize value) |> Async.AwaitTask
        return Some value }

    member __.Delete (key: 'TKey) = async {
      let filePath = Path.Combine(path, keyFormatter key)

      do File.Delete filePath
      return () }

type DirectoryRepository =
  static member Create<'TKey, 'TValue when 'TKey: comparison>(directoryName, ?keyFormatter) =
    let keyFormatter = defaultArg keyFormatter (fun (key:'TKey) -> sprintf "%A" key)
    new DirectoryRepository<'TKey, 'TValue>(directoryName, keyFormatter) :> IRepository<'TKey, 'TValue>
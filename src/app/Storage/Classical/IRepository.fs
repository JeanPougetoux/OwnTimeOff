namespace Storage.Classical

exception KeyExistsException

type IRepository<'TKey, 'TValue when 'TKey: comparison> =
  abstract member Create: key: 'TKey -> value: 'TValue ->  Async<'TValue>
  abstract member Read: key: 'TKey -> Async<'TValue option>
  abstract member ReadAll: unit -> Async<'TValue seq>
  abstract member Update: key: 'TKey -> value: 'TValue ->  Async<'TValue option>
  abstract member Delete: key: 'TKey ->  Async<unit>

So as I understand it your example states:

- The `BCollection` contains items of `ClassB`.
- `ClassB` has a member named `C` that is a `ClassC`.
- `ClassC` implements `INotifyPropertyChanged` and in particular will raise this event when `C.Cost` changes.

And finally, we have `ClassA` which contains the `BCollection` and has a `SumOfBCost` that should be updated whenever:

1. `C.Cost` changes
2. `ClassB.C` instance is replaced with a new instance of `ClassC`. (Note that this is 'not' a collection changed event when this occurs!)
3. `BCollection` undergoes changes of `ClassB` items that are added, removed or replaced. (All of which _are_ collection changed events!)

___

As an alternative to subclassing `ObservableCollection<T>`, the snippet below (and the GitHub [repo that I used to test this answer]()) employ the `WithNotifyOnDescendants(...)` extension that is a part of the [XBoundObject NuGet package](https://www.nuget.org/packages/IVSoftware.Portable.Xml.Linq.XBoundObject) whose source code can be found [here](https://github.com/IVSoftware/IVSoftware.Portable.Xml.Linq.XBoundObject.git).



[![screenshot][1]][1]


  [1]: https://i.sstatic.net/65g1zRRB.png
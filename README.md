# Oracle Loader

A generic implementation of bulk insert for Oracle using array binding.

## Build

```bash
build.bat
```

A nuget package named OracleLoader.version.nupkg will be created.

## Usage

```c#
var loader = new OracleLoader();
loader.Connection = <pass your opened connection here>;
loader.TableName = <put your table name here>;
// if you want to disable before insert and enable it back after
// default value is true
loader.DisableConstraints = true;
// buffer size -> the count of entities that will be inserted pro time
// default valie is 100
loader.BufferSize = 100;
loader.Open();
foreach (var element in collection) {
    loader.SetValue(<column name>, <value>);
    ...
    loader.NextRow();
}
loader.Close();
```

## Exceptions

- TableNameNotSetException if you did forgot to set the table name
- ColumnNotFoundException if the table haven't the column used in SetValue
- NullValueNotAllowedException if you try to pass the null value into column that is declared as not null
- TypeIncompatibleException if you try to pass the value which type is not compatible with the type of the target column
- ORA-xxx if something goes wrong
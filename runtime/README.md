# Runtime

As you may have read in the readme, there are runtime methods that need to be implemented. There is a default implementation in this folder, but if you wish, you can replace it with your own.

### Arrays

Arrays are allocated through an *newarr* method. The array layout can be found in the following table.

| Offset | Name | Description |
| ------ | ---- | ----------- |
| 0 | Length | The amount of elements in this array (32-bit integer) |
| 4 (32-bit), 8 (64-bit) | Elements | The elements of this array |

The elements need to be aligned on a natural boundary for the platform.
The *newarr* method returns a void-pointer to the elements of the array. It allocates memory for the array structure as seen above, zeroes the elements, and sets the length. This is one of the runtime methods.
#include <stdlib.h>
#include <string.h>
#include <stdint.h>

void* newarr(int32_t nelements, size_t size)
{
    int32_t* ptr = malloc(sizeof(int32_t) + (nelements * size));
    if(ptr == NULL)
        return NULL;
    *ptr = nelements;
    return (void*)(ptr + 1);
}
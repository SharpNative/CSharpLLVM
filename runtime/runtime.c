#include <stdlib.h>
#include <string.h>
#include <stdint.h>

void* newarr(int32_t nelements, size_t size)
{
	// Note: An array length is a 32-bit int, but we need to align on native alignment
    int32_t* ptr = malloc(sizeof(size_t) + (nelements * size));
    if(ptr == NULL)
        return NULL;
    *ptr = nelements;
    void* ret = (void*)((int8_t*)ptr + sizeof(size_t));;
    memset(ret, 0, nelements * size);
    return ret;
}
#include <stdbool.h>
#include <stddef.h>

// Sincroniza I-cache/D-cache após reescrever código (obrigatório em ARM pós-patch;
// sem isso a CPU pode executar código velho → corrupção silenciosa).
bool FlushICache(void* start, void* end) {
    __builtin___clear_cache((char*)start, (char*)end);
    return true;
}

#include <stdbool.h>
#include <stddef.h>
#include <sys/mman.h>
#include <unistd.h>

// Sincroniza I-cache/D-cache após reescrever código (obrigatório em ARM pós-patch;
// sem isso a CPU pode executar código velho → corrupção silenciosa).
bool FlushICache(void* start, void* end) {
    __builtin___clear_cache((char*)start, (char*)end);
    return true;
}

// TEMPORÁRIO (Chunk 2 / gate Risco 3): valida memória executável anônima no domínio do app.
// 0 = ok (mmap+mprotect RX); 1 = mmap falhou; 2 = mprotect(PROT_EXEC) negado (EPERM).
int TestExecMem(void) {
    size_t sz = (size_t)sysconf(_SC_PAGESIZE);
    void* p = mmap(NULL, sz, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
    if (p == MAP_FAILED) return 1;
    int r = mprotect(p, sz, PROT_READ | PROT_EXEC) != 0 ? 2 : 0;
    munmap(p, sz);
    return r;
}

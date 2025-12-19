#ifndef VANITYSEARCH_WRAPPER_H
#define VANITYSEARCH_WRAPPER_H

#ifdef __cplusplus
extern "C" {
#endif

#ifdef _WIN32
    #define EXPORT __declspec(dllexport)
#else
    #define EXPORT __attribute__((visibility("default")))
#endif

// Result structure for vanity search
typedef struct {
    int found;                  // 1 if found, 0 if not
    char mnemonic[256];         // BIP39 mnemonic phrase
    char address[64];           // PoCX address (pocx1q...)
    unsigned long attempts;     // Number of attempts made
    double elapsed_seconds;     // Time elapsed
} VanitySearchResult;

// Callback for progress updates
typedef void (*ProgressCallback)(unsigned long attempts, double rate);

/**
 * Initialize CUDA/GPU for vanity search
 * Returns 0 on success, negative on error
 */
EXPORT int vanitysearch_init();

/**
 * Search for vanity address matching pattern
 * 
 * @param pattern - Pattern to search for (e.g., "madf0x")
 * @param use_gpu - 1 to use GPU, 0 for CPU only
 * @param max_attempts - Maximum attempts (0 = unlimited)
 * @param progress_cb - Optional progress callback
 * @param result - Output result structure
 * @return 0 on success, negative on error
 */
EXPORT int vanitysearch_find(
    const char* pattern,
    int use_gpu,
    unsigned long max_attempts,
    ProgressCallback progress_cb,
    VanitySearchResult* result);

/**
 * Stop ongoing search
 */
EXPORT void vanitysearch_stop();

/**
 * Cleanup and release resources
 */
EXPORT void vanitysearch_cleanup();

/**
 * Get last error message
 */
EXPORT const char* vanitysearch_get_error();

#ifdef __cplusplus
}
#endif

#endif // VANITYSEARCH_WRAPPER_H

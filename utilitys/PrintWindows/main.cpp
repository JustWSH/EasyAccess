#include <windows.h>
#include <commctrl.h>
#include <cstdio>

constexpr int HOTKEY_PRINT = 1;
constexpr int HOTKEY_QUIT = 2;

// Safe print with fflush
void safePrint(const char* fmt, ...) {
    va_list args;
    va_start(args, fmt);
    vprintf(fmt, args);
    va_end(args);
    fflush(stdout);
}

// Print window tree (using printf)
void printWindowTree(HWND hwnd, int depth, int maxDepth) {
    if (depth > maxDepth) return;
    if (!IsWindowVisible(hwnd) && depth > 0) return;

    // Get info
    wchar_t classBuf[128] = {};
    wchar_t titleBuf[256] = {};
    GetClassNameW(hwnd, classBuf, 128);
    GetWindowTextW(hwnd, titleBuf, 256);

    char classNarrow[128] = {};
    char titleNarrow[256] = {};
    WideCharToMultiByte(CP_UTF8, 0, classBuf, -1, classNarrow, 128, nullptr, nullptr);
    WideCharToMultiByte(CP_UTF8, 0, titleBuf, -1, titleNarrow, 256, nullptr, nullptr);

    int ctrlId = GetDlgCtrlID(hwnd);
    RECT rc = {};
    GetWindowRect(hwnd, &rc);

    // Indent with tree symbols
    for (int i = 0; i < depth; i++) {
        if (i == depth - 1)
            safePrint("├─ ");
        else
            safePrint("│  ");
    }

    // Output with handle
    safePrint("[0x%p] %s", hwnd, classNarrow);
    if (titleNarrow[0]) {
        // Truncate long titles
        if (strlen(titleNarrow) > 50) {
            titleNarrow[47] = '.';
            titleNarrow[48] = '.';
            titleNarrow[49] = '.';
            titleNarrow[50] = '\0';
        }
        safePrint(" \"%s\"", titleNarrow);
    }
    if (ctrlId) safePrint(" (ID=%d)", ctrlId);
    safePrint(" [%d,%d %dx%d]", rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);
    safePrint("\n");

    // Children
    HWND child = GetWindow(hwnd, GW_CHILD);
    int count = 0;
    while (child && count < 200) {
        printWindowTree(child, depth + 1, maxDepth);
        child = GetWindow(child, GW_HWNDNEXT);
        count++;
    }
}

// Print window info
void printWindowInfo(HWND hwnd) {
    safePrint("\n=== Capture Start ===\n");

    if (!hwnd) {
        safePrint("NULL handle\n");
        return;
    }

    if (!IsWindow(hwnd)) {
        safePrint("Invalid window\n");
        return;
    }

    // Get basic info
    wchar_t titleBuf[256] = {};
    wchar_t classBuf[128] = {};
    GetWindowTextW(hwnd, titleBuf, 256);
    GetClassNameW(hwnd, classBuf, 128);

    char titleNarrow[256] = {};
    char classNarrow[128] = {};
    WideCharToMultiByte(CP_UTF8, 0, titleBuf, -1, titleNarrow, 256, nullptr, nullptr);
    WideCharToMultiByte(CP_UTF8, 0, classBuf, -1, classNarrow, 128, nullptr, nullptr);

    DWORD pid = 0;
    GetWindowThreadProcessId(hwnd, &pid);

    RECT rc = {};
    GetWindowRect(hwnd, &rc);

    DWORD style = GetWindowLongW(hwnd, GWL_STYLE);

    // Print info
    safePrint("Handle: 0x%p\n", hwnd);
    safePrint("Title: %s\n", titleNarrow);
    safePrint("Class: %s\n", classNarrow);
    safePrint("PID: %lu\n", pid);
    safePrint("Rect: (%d,%d) size: %dx%d\n", rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);
    safePrint("Style: 0x%lX\n", style);

    // Process name
    HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, pid);
    if (hProcess) {
        wchar_t procPath[MAX_PATH] = {};
        DWORD size = MAX_PATH;
        if (QueryFullProcessImageNameW(hProcess, 0, procPath, &size)) {
            char procNarrow[MAX_PATH] = {};
            WideCharToMultiByte(CP_UTF8, 0, procPath, -1, procNarrow, MAX_PATH, nullptr, nullptr);
            safePrint("Process: %s\n", procNarrow);
        }
        CloseHandle(hProcess);
    }

    safePrint("\n--- Window Tree ---\n");
    printWindowTree(hwnd, 0, 8);
    safePrint("=== Capture End ===\n\n");
}

int main() {
    SetConsoleOutputCP(CP_UTF8);

    safePrint("PrintWindows - Window Element Viewer\n");
    safePrint("-------------------------------------\n");
    safePrint("F9  = Print foreground window\n");
    safePrint("F10 = Quit\n");
    safePrint("-------------------------------------\n");

    if (!RegisterHotKey(nullptr, HOTKEY_PRINT, 0, VK_F9)) {
        printf("F9 register failed: %lu\n", GetLastError());
        printf("Press Enter to exit...");
        getchar();
        return 1;
    }
    safePrint("F9 OK\n");

    if (!RegisterHotKey(nullptr, HOTKEY_QUIT, 0, VK_F10)) {
        printf("F10 register failed: %lu\n", GetLastError());
        UnregisterHotKey(nullptr, HOTKEY_PRINT);
        printf("Press Enter to exit...");
        getchar();
        return 1;
    }
    safePrint("F10 OK\n");
    safePrint("\nReady. Press F9 to capture.\n\n");

    MSG msg;
    while (GetMessage(&msg, nullptr, 0, 0)) {
        if (msg.message == WM_HOTKEY) {
            if (msg.wParam == HOTKEY_PRINT) {
                HWND fg = GetForegroundWindow();
                printWindowInfo(fg);
            } else if (msg.wParam == HOTKEY_QUIT) {
                break;
            }
        }
    }

    UnregisterHotKey(nullptr, HOTKEY_PRINT);
    UnregisterHotKey(nullptr, HOTKEY_QUIT);
    return 0;
}

#include <windows.h>
#include <UIAutomation.h>
#include <cstdio>
#include <string>

// Recursively print UIA tree
void printUiaTree(IUIAutomation* uia, IUIAutomationElement* elem, int depth, int maxDepth) {
    if (depth > maxDepth) return;

    BSTR name = nullptr;
    BSTR className = nullptr;
    BSTR autoId = nullptr;
    int controlType = 0;

    elem->get_CurrentName(&name);
    elem->get_CurrentClassName(&className);
    elem->get_CurrentAutomationId(&autoId);
    elem->get_CurrentControlType(&controlType);

    // Indent
    for (int i = 0; i < depth; i++) {
        if (i == depth - 1) printf("+- ");
        else printf("|  ");
    }

    // Control type name
    const char* typeName = "Unknown";
    switch (controlType) {
        case UIA_ButtonControlTypeId: typeName = "Button"; break;
        case UIA_EditControlTypeId: typeName = "Edit"; break;
        case UIA_ComboBoxControlTypeId: typeName = "ComboBox"; break;
        case UIA_TextControlTypeId: typeName = "Text"; break;
        case UIA_ListControlTypeId: typeName = "List"; break;
        case UIA_ListItemControlTypeId: typeName = "ListItem"; break;
        case UIA_WindowControlTypeId: typeName = "Window"; break;
        case UIA_PaneControlTypeId: typeName = "Pane"; break;
        case UIA_GroupControlTypeId: typeName = "Group"; break;
        case UIA_TabControlTypeId: typeName = "Tab"; break;
        case UIA_TabItemControlTypeId: typeName = "TabItem"; break;
        case UIA_MenuControlTypeId: typeName = "Menu"; break;
        case UIA_MenuItemControlTypeId: typeName = "MenuItem"; break;
        case UIA_TreeControlTypeId: typeName = "Tree"; break;
        case UIA_TreeItemControlTypeId: typeName = "TreeItem"; break;
        case UIA_ToolBarControlTypeId: typeName = "ToolBar"; break;
        case UIA_HyperlinkControlTypeId: typeName = "Hyperlink"; break;
        case UIA_CheckBoxControlTypeId: typeName = "CheckBox"; break;
        case UIA_RadioButtonControlTypeId: typeName = "RadioButton"; break;
        case UIA_SliderControlTypeId: typeName = "Slider"; break;
        case UIA_ProgressBarControlTypeId: typeName = "ProgressBar"; break;
        case UIA_CustomControlTypeId: typeName = "Custom"; break;
        case UIA_DocumentControlTypeId: typeName = "Document"; break;
        case UIA_HeaderControlTypeId: typeName = "Header"; break;
        case UIA_HeaderItemControlTypeId: typeName = "HeaderItem"; break;
        case UIA_DataGridControlTypeId: typeName = "DataGrid"; break;
        case UIA_SeparatorControlTypeId: typeName = "Separator"; break;
        case UIA_SemanticZoomControlTypeId: typeName = "SemanticZoom"; break;
        case UIA_AppBarControlTypeId: typeName = "AppBar"; break;
        case UIA_StatusBarControlTypeId: typeName = "StatusBar"; break;
        case UIA_SpinnerControlTypeId: typeName = "Spinner"; break;
        case UIA_CalendarControlTypeId: typeName = "Calendar"; break;
        case UIA_ImageControlTypeId: typeName = "Image"; break;
    }

    // Check for ValuePattern
    bool hasValue = false;
    void* valuePattern = nullptr;
    if (SUCCEEDED(elem->GetCurrentPatternAs(UIA_ValuePatternId, IID_IUIAutomationValuePattern, &valuePattern)) && valuePattern) {
        hasValue = true;
        static_cast<IUnknown*>(valuePattern)->Release();
    }

    // Check for InvokePattern
    bool hasInvoke = false;
    void* invokePattern = nullptr;
    if (SUCCEEDED(elem->GetCurrentPatternAs(UIA_InvokePatternId, IID_IUIAutomationInvokePattern, &invokePattern)) && invokePattern) {
        hasInvoke = true;
        static_cast<IUnknown*>(invokePattern)->Release();
    }

    // Build pattern string
    std::string patterns;
    if (hasValue) patterns += "[Value]";
    if (hasInvoke) patterns += "[Invoke]";

    // Print
    printf("[%s] %s", typeName, patterns.c_str());
    if (name) {
        wprintf(L" name=\"%s\"", name);
        SysFreeString(name);
    }
    if (autoId && SysStringLen(autoId) > 0) {
        wprintf(L" autoId=\"%s\"", autoId);
        SysFreeString(autoId);
    }
    if (className && SysStringLen(className) > 0) {
        wprintf(L" class=\"%s\"", className);
        SysFreeString(className);
    }

    printf("\n");
    fflush(stdout);

    // Get children
    IUIAutomationElement* child = nullptr;
    IUIAutomationCondition* trueCond = nullptr;
    uia->CreateTrueCondition(&trueCond);

    IUIAutomationElementArray* children = nullptr;
    if (SUCCEEDED(elem->FindAll(TreeScope_Children, trueCond, &children)) && children) {
        int count = 0;
        children->get_Length(&count);
        for (int i = 0; i < count && i < 200; i++) {
            IUIAutomationElement* c = nullptr;
            children->GetElement(i, &c);
            if (c) {
                printUiaTree(uia, c, depth + 1, maxDepth);
                c->Release();
            }
        }
        children->Release();
    }
    if (trueCond) trueCond->Release();
}

int main() {
    SetConsoleOutputCP(CP_UTF8);
    printf("ProbeUia - UI Automation Tree Viewer\n");
    printf("-------------------------------------\n");
    printf("F9  = Probe foreground window UIA tree\n");
    printf("F10 = Quit\n");
    printf("-------------------------------------\n\n");

    // Init COM
    CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);

    IUIAutomation* uia = nullptr;
    HRESULT hr = CoCreateInstance(CLSID_CUIAutomation, nullptr, CLSCTX_INPROC_SERVER,
                                  IID_IUIAutomation, (void**)&uia);
    if (FAILED(hr) || !uia) {
        printf("Failed to create UIA instance: 0x%08X\n", (unsigned)hr);
        return 1;
    }
    printf("UIA initialized OK\n\n");

    RegisterHotKey(nullptr, 1, 0, VK_F9);
    RegisterHotKey(nullptr, 2, 0, VK_F10);

    MSG msg;
    while (GetMessage(&msg, nullptr, 0, 0)) {
        if (msg.message == WM_HOTKEY) {
            if (msg.wParam == 1) {
                HWND fg = GetForegroundWindow();
                if (!fg) { printf("No foreground window\n"); continue; }

                wchar_t title[256] = {};
                GetWindowTextW(fg, title, 256);
                wchar_t cls[128] = {};
                GetClassNameW(fg, cls, 128);
                wprintf(L"\n=== Probing: %s [%s] ===\n", title, cls);

                IUIAutomationElement* root = nullptr;
                hr = uia->ElementFromHandle(fg, &root);
                if (FAILED(hr) || !root) {
                    printf("ElementFromHandle failed: 0x%08X\n", (unsigned)hr);
                    continue;
                }

                printUiaTree(uia, root, 0, 10);
                printf("=== End ===\n\n");
                root->Release();

            } else if (msg.wParam == 2) {
                break;
            }
        }
    }

    uia->Release();
    CoUninitialize();
    UnregisterHotKey(nullptr, 1);
    UnregisterHotKey(nullptr, 2);
    return 0;
}

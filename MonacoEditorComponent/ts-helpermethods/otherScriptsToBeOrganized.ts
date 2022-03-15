﻿///<reference path="../monaco-editor/monaco.d.ts" />
declare var Accessor: ParentAccessor;
declare var Keyboard: KeyboardListener;

declare var editor: monaco.editor.IStandaloneCodeEditor;
declare var model: monaco.editor.ITextModel;
declare var contexts: { [index: string]: monaco.editor.IContextKey<any> };//{};
declare var decorations: string[];
declare var modifingSelection: boolean; // Supress updates to selection when making edits.

const registerHoverProvider = function (languageId: string) {
    return monaco.languages.registerHoverProvider(languageId, {
        provideHover: function (model, position) {
            return Accessor.callEvent("HoverProvider" + languageId, [JSON.stringify(position)]).then(result => {
                if (result) {
                    return JSON.parse(result);
                }
            });
        }
    });
};

const addAction = function (action: monaco.editor.IActionDescriptor) {
    action.run = function (ed) {
        Accessor.callAction("Action" + action.id)
    };

    editor.addAction(action);
};

const addCommand = function (keybindingStr, handlerName, context) {
    return editor.addCommand(parseInt(keybindingStr), function () {
        const objs = [];
        if (arguments) { // Use arguments as Monaco will pass each as it's own parameter, so we don't know how many that may be.
            for (let i = 1; i < arguments.length; i++) { // Skip first one as that's the sender?
                objs.push(JSON.stringify(arguments[i]));
            }
        }
        Accessor.callActionWithParameters(handlerName, objs);
    }, context);
};

const createContext = function (context) {
    if (context) {
        contexts[context.key] = editor.createContextKey(context.key, context.defaultValue);
    }
};

const updateContext = function (key, value) {
    contexts[key].set(value);
}

// link:CodeEditor.Properties.cs:updateContent
const updateContent = function (content) {
    // Need to ignore updates from us notifying of a change
    if (content !== model.getValue()) {
        model.setValue(content);
    }
};







const updateDecorations = function (newHighlights) {
    if (newHighlights) {
        decorations = editor.deltaDecorations(decorations, newHighlights);
    } else {
        decorations = editor.deltaDecorations(decorations, []);
    }
};

const updateStyle = function (innerStyle) {
    var style = document.getElementById("dynamic");
    style.innerHTML = innerStyle;
};

const getOptions = async function (): Promise<monaco.editor.IEditorOptions> {
    let opt = null;
    try {
        opt = JSON.parse(await Accessor.getJsonValue("Options"));
    } finally {

    }

    if (opt !== null && typeof opt === "object") {
        return opt;
    }

    return {};
};

const updateOptions = function (opt: monaco.editor.IEditorOptions) {
    if (opt !== null && typeof opt === "object") {
        editor.updateOptions(opt);
    }
};

const updateLanguage = function (language) {
    monaco.editor.setModelLanguage(model, language);
};

const changeTheme = function (theme: string, highcontrast) {
    let newTheme = 'vs';
    if (highcontrast == "True" || highcontrast == "true") {
        newTheme = 'hc-black';
    } else if (theme == "Dark") {
        newTheme = 'vs-dark';
    }

    monaco.editor.setTheme(newTheme);
};



const keyDown = async function (event) {
    //Debug.log("Key Down:" + event.keyCode + " " + event.ctrlKey);
    const result = await Keyboard.keyDown(event.keyCode, event.ctrlKey, event.shiftKey, event.altKey, event.metaKey);
    if (result) {
        event.cancelBubble = true;
        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation();
        return false;
    }
};

﻿///<reference path="../monaco-editor/monaco.d.ts" />
declare var Accessor: ParentAccessor;

function isTextEdit(edit: monaco.languages.WorkspaceTextEdit | monaco.languages.WorkspaceFileEdit): edit is monaco.languages.WorkspaceTextEdit {
    return (edit as monaco.languages.WorkspaceTextEdit).edit !== undefined;
}

const registerCodeActionProvider = function (languageId) {
    return monaco.languages.registerCodeActionProvider(languageId, {
        provideCodeActions: function (model, range, context, token) {
            return Accessor.callEvent("ProvideCodeActions" + languageId, [JSON.stringify(range), JSON.stringify(context)]).then(result => {
                if (result) {
                    const list: monaco.languages.CodeActionList = JSON.parse(result);

                    // Need to add in the model.uri to any edits to connect the dots
                    if (list.actions &&
                        list.actions.length > 0) {
                        list.actions.forEach((action) => {
                            if (action.edit &&
                                action.edit.edits &&
                                action.edit.edits.length > 0) {
                                action.edit.edits.forEach((inneredit) => {
                                    if (isTextEdit(inneredit)) {
                                        inneredit.resource = model.uri;
                                    }
                                });
                            }
                        });
                    }

                    // Add dispose method for IDisposable that Monaco is looking for.
                    list.dispose = () => {};

                    return list;
                }
            });
        },
    });
}
﻿using Microsoft.Toolkit.Uwp;
using Monaco.Editor;
using Monaco.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

#if !NETSTANDARD2_0
using System.Runtime.InteropServices.WindowsRuntime;
#else
using ReadOnlyArrayAttribute = Monaco.Helpers.Stubs.ReadOnlyArrayAttribute;
#endif

namespace Monaco
{
    /// <summary>
    /// Action delegate for <see cref="CodeEditor.AddCommandAsync(int, CommandHandler)"/> and <see cref="CodeEditor.AddCommandAsync(int, CommandHandler, string)"/>.
    /// </summary>
    public delegate void CommandHandler([ReadOnlyArray] object[] parameters);

    /// <summary>
    /// This file contains Monaco IEditor method implementations we can call on our control.
    /// https://microsoft.github.io/monaco-editor/api/interfaces/monaco.editor.ieditor.html
    /// https://microsoft.github.io/monaco-editor/api/interfaces/monaco.editor.icommoncodeeditor.html
    /// </summary>
    public partial class CodeEditor
    {
#region Reveal Methods
        public IAsyncAction RevealLineAsync(uint lineNumber)
        {
            return SendScriptAsync("editor.revealLine(" + lineNumber + ")").AsAsyncAction();
        }

        public IAsyncAction RevealLineInCenterAsync(uint lineNumber)
        {
            return SendScriptAsync("editor.revealLineInCenter(" + lineNumber + ")").AsAsyncAction();
        }

        public IAsyncAction RevealLineInCenterIfOutsideViewportAsync(uint lineNumber)
        {
            return SendScriptAsync("editor.revealLineInCenterIfOutsideViewport(" + lineNumber + ")").AsAsyncAction();
        }

        public IAsyncAction RevealLinesAsync(uint startLineNumber, uint endLineNumber)
        {
            return SendScriptAsync("editor.revealLines(" + startLineNumber + ", " + endLineNumber + ")").AsAsyncAction();
        }

        public IAsyncAction RevealLinesInCenterAsync(uint startLineNumber, uint endLineNumber)
        {
            return SendScriptAsync("editor.revealLinesInCenter(" + startLineNumber + ", " + endLineNumber + ")").AsAsyncAction();
        }

        public IAsyncAction RevealLinesInCenterIfOutsideViewportAsync(uint startLineNumber, uint endLineNumber)
        {
            return SendScriptAsync("editor.revealLinesInCenterIfOutsideViewport(" + startLineNumber + ", " + endLineNumber + ")").AsAsyncAction();
        }

        public IAsyncAction RevealPositionAsync(IPosition position)
        {
            return RevealPositionAsync(position, false, false);
        }

        public IAsyncAction RevealPositionAsync(IPosition position, bool revealVerticalInCenter)
        {
            return RevealPositionAsync(position, revealVerticalInCenter, false);
        }

        public IAsyncAction RevealPositionAsync(IPosition position, bool revealVerticalInCenter, bool revealHorizontal)
        {
            return SendScriptAsync("editor.revealPosition(JSON.parse('" + JsonConvert.SerializeObject(position) + "'), " + JsonConvert.ToString(revealVerticalInCenter) + ", " + JsonConvert.ToString(revealHorizontal) + ")").AsAsyncAction();
        }

        public IAsyncAction RevealPositionInCenterAsync(IPosition position)
        {
            return SendScriptAsync("editor.revealPositionInCenter(JSON.parse('" + JsonConvert.SerializeObject(position) + "'))").AsAsyncAction();
        }

        public IAsyncAction RevealPositionInCenterIfOutsideViewportAsync(IPosition position)
        {
            return SendScriptAsync("editor.revealPositionInCenterIfOutsideViewport(JSON.parse('" + JsonConvert.SerializeObject(position) + "'))").AsAsyncAction();
        }

        public IAsyncAction RevealRangeAsync(IRange range)
        {
            return SendScriptAsync("editor.revealRange(JSON.parse('" + JsonConvert.SerializeObject(range) + "'))").AsAsyncAction();
        }

        public IAsyncAction RevealRangeAtTopAsync(IRange range)
        {
            return SendScriptAsync("editor.revealRangeAtTop(JSON.parse('" + JsonConvert.SerializeObject(range) + "'))").AsAsyncAction();
        }

        public IAsyncAction RevealRangeInCenterAsync(IRange range)
        {
            return SendScriptAsync("editor.revealRangeInCenter(JSON.parse('" + JsonConvert.SerializeObject(range) + "'))").AsAsyncAction();
        }

        public IAsyncAction RevealRangeInCenterIfOutsideViewportAsync(IRange range)
        {
            return SendScriptAsync("editor.revealRangeInCenterIfOutsideViewport(JSON.parse('" + JsonConvert.SerializeObject(range) + "'))").AsAsyncAction();
        }
#endregion

        public IAsyncAction AddActionAsync(IActionDescriptor action)
        {
            var wref = new WeakReference<CodeEditor>(this);
            _parentAccessor.RegisterAction("Action" + action.Id, new Action(() => { if (wref.TryGetTarget(out CodeEditor editor)) { action?.Run(editor, null); } }));
            return InvokeScriptAsync("addAction", action).AsAsyncAction();
        }

        /// <summary>
        /// Invoke scripts, return value must be strings
        /// </summary>
        /// <param name="script">Script to invoke</param>
        /// <returns>An async operation result to string</returns>
        public IAsyncOperation<string> InvokeScriptAsync(string script)
        {
            return _view.InvokeScriptAsync("eval", new[] { script });
        }

        private int _commandIndex = 0;

        public IAsyncOperation<string> AddCommandAsync(CommandHandler handler)
        {
            return AddCommandAsync(0, handler, string.Empty);
        }

        public IAsyncOperation<string> AddCommandAsync(int keybinding, CommandHandler handler)
        {
            return AddCommandAsync(keybinding, handler, string.Empty);
        }

        public IAsyncOperation<string> AddCommandAsync(int keybinding, CommandHandler handler, string context)
        {
            var name = "Command" + Interlocked.Increment(ref _commandIndex);
            _parentAccessor.RegisterActionWithParameters(name, (parameters) => 
            {
                if (parameters != null && parameters.Length > 0)
                {
                    object[] args = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        args[i] = JsonConvert.DeserializeObject<object>(parameters[i]);
                    }

                    handler?.Invoke(args);
                }
                else
                {
                    handler?.Invoke(new object[] {});
                }
            });
            return InvokeScriptAsync<string>("addCommand", new object[] { keybinding, name, context }).AsAsyncOperation();
        }

        public async Task<ContextKey> CreateContextKeyAsync(string key, bool defaultValue)
        {
            var ck = new ContextKey(this, key, defaultValue);

            await InvokeScriptAsync("createContext", ck);

            return ck;
        }

        public IModel GetModel()
        {
            return _model ?? throw new NotSupportedException($"Model is not available");
        }

        public IAsyncOperation<IEnumerable<Marker>> GetModelMarkersAsync() // TODO: Filter (string? owner, Uri? resource, int? take)
        {
            return SendScriptAsync<IEnumerable<Marker>>("monaco.editor.getModelMarkers();").AsAsyncOperation();
        }

        public IAsyncAction SetModelMarkersAsync(string owner, [ReadOnlyArray] IMarkerData[] markers)
        {
            return SendScriptAsync("monaco.editor.setModelMarkers(EditorContext.getEditorForElement(element).model, " + JsonConvert.ToString(owner) + ", " + JsonConvert.SerializeObject(markers) + ");").AsAsyncAction();
        }

        public IAsyncOperation<Position> GetPositionAsync()
        {
            return SendScriptAsync<Position>("EditorContext.getEditorForElement(element).editor.getPosition();").AsAsyncOperation();
        }

        public IAsyncAction SetPositionAsync(IPosition position)
        {
            return SendScriptAsync("EditorContext.getEditorForElement(element).editor.setPosition(" + JsonConvert.SerializeObject(position) + ");").AsAsyncAction();
        }

        /// <summary>
        /// https://microsoft.github.io/monaco-editor/api/interfaces/monaco.editor.icommoncodeeditor.html#deltadecorations
        /// 
        /// Using <see cref="Decorations"/> Property to manipulate decorations instead of calling this directly.
        /// </summary>
        /// <param name="newDecorations"></param>
        /// <returns></returns>
        private async Task DeltaDecorationsHelperAsync([ReadOnlyArray] IModelDeltaDecoration[] newDecorations)
        {
            await _queue.EnqueueAsync(async () =>
            {
                var newDecorationsAdjust = newDecorations ?? Array.Empty<IModelDeltaDecoration>();

                if (_cssBroker.AssociateStyles(newDecorations))
                {
                        // Update Styles First
                        await InvokeScriptAsync("updateStyle", _cssBroker.GetStyles());
                }

                    // Send Command to Modify Decorations
                    // IMPORTANT: Need to cast to object here as we want this to be a single array object passed as a parameter, not a list of parameters to expand.
                    await InvokeScriptAsync("updateDecorations", (object)newDecorationsAdjust);
            });
        }
    }
}

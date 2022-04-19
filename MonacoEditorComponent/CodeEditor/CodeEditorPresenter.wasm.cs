using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Monaco.Extensions;
using Uno.Foundation;
using Uno.Foundation.Interop;
using Uno.Logging;
using Uno.Extensions;
using Uno.UI.Runtime.WebAssembly;

namespace Monaco
{
	[HtmlElement("div")]
    public partial class CodeEditorPresenter : Control, ICodeEditorPresenter, IJSObject
	{
		private static readonly string UNO_BOOTSTRAP_APP_BASE = global::System.Environment.GetEnvironmentVariable(nameof(UNO_BOOTSTRAP_APP_BASE));
		private static readonly string UNO_BOOTSTRAP_WEBAPP_BASE_PATH = Environment.GetEnvironmentVariable(nameof(UNO_BOOTSTRAP_WEBAPP_BASE_PATH)) ?? "";

		private readonly JSObjectHandle _handle;

		/// <inheritdoc />
		JSObjectHandle IJSObject.Handle => _handle;

		public CodeEditorPresenter()
		{
			//Background = new SolidColorBrush(Colors.Red);
			_handle = JSObjectHandle.Create(this);

			RaiseDOMContentLoaded();


			//WebAssemblyRuntime.InvokeJSWithInterop($@"
			//	console.log(""///////////////////////////////// subscribing to DOMContentLoaded - "" + {HtmlId});

			//	var frame = Uno.UI.WindowManager.current.getView({HtmlId});
				
			//	console.log(""Got view"");

			//	frame.addEventListener(""loadstart"", function(event) {{
			//		var frameDoc = frame.contentDocument;
			//		console.log(""/////////////////////////////////  Frame DOMContentLoaded, subscribing to document"" + frameDoc);
			//		{this}.RaiseDOMContentLoaded();
			//	}}); 
			//	console.log(""Added load start"");



			//	frame.addEventListener(""load"", function(event) {{
			//		var frameDoc = frame.contentDocument;
			//		console.log(""/////////////////////////////////  Frame loaded, subscribing to document"" + frameDoc);
			//		{this}.RaiseDOMContentLoaded();
			//		//frameDoc.addEventListener(""DOMContentLoaded"", function(event) {{
			//		//	console.log(""Raising RaiseDOMContentLoaded"");
			//		//	{this}.RaiseDOMContentLoaded();
			//		//}});
			//	}}); 

			//	console.log(""Added load"");


			//	");
		}

		public void RaiseDOMContentLoaded()
		{
            Console.WriteLine("RaiseDOMContentLoaded");
			this.Log().Info($"RaiseDOMContentLoaded: Handle is null {_handle == null}");

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
			{
				this.Log().Debug($"RaiseDOMContentLoaded: Handle is null {_handle == null}");
			}

			if (_handle == null) return;

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
			{
				this.Log().Debug($"Raising DOMContentLoaded");
			}

			Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => DOMContentLoaded?.Invoke(null, new WebViewDOMContentLoadedEventArgs()));
		}

		/// <inheritdoc />
		public void AddWebAllowedObject(string name, object pObject)
		{
			if (pObject is IJSObject obj)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
				{
					this.Log().Debug($"AddWebAllowedObject: Add Web Allowed Object - {name}");
				}

				var method = obj.Handle.GetType().GetMethod("GetNativeInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
				{
					this.Log().Debug($"AddWebAllowedObject: Method exists {method != null}");
				}

				var native  = method.Invoke(obj.Handle,new object[] { }) as string;

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
				{
					this.Log().Debug($"AddWebAllowedObject: Native handle {native}");
				}

                var htmlId = this.GetHtmlId();

				var script = $@"
					// console.log('starting');
					var value = {native};
					// console.log('v>' + value);
					var frame = Uno.UI.WindowManager.current.getView({htmlId});
					// console.log('f>' + (!frame));
					var frameWindow = window;
					// console.log('fw>' + (!frameWindow));

					var editorContext = EditorContext.getEditorForElement(frame);
					editorContext.{name} = value;

					// console.log('ended');
					";

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
				{
					this.Log().Debug($"AddWebAllowedObject: {script}");
				}

                try
                {
                    WebAssemblyRuntime.InvokeJS(script);
                }
                catch (Exception e)
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
					{
						this.Log().Error($"AddWebAllowedObject failed", e);
					}
                }

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
				{
					this.Log().Debug($"Add WebAllowed Compeleted");
				}
			}
			else
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
				{
					this.Log().Error($"AddWebAllowedObject: {name} is not a JSObject");
				}
			}
		}

		/// <inheritdoc />
		public event TypedEventHandler<ICodeEditorPresenter, WebViewNewWindowRequestedEventArgs> NewWindowRequested; // ignored for now (external navigation)

		/// <inheritdoc />
		public event TypedEventHandler<ICodeEditorPresenter, WebViewNavigationStartingEventArgs> NavigationStarting;

		/// <inheritdoc />
		public event TypedEventHandler<ICodeEditorPresenter, WebViewDOMContentLoadedEventArgs> DOMContentLoaded;

		/// <inheritdoc />
		public event TypedEventHandler<ICodeEditorPresenter, WebViewNavigationCompletedEventArgs> NavigationCompleted; // ignored for now (only focus the editor)

		/// <inheritdoc />
		public global::System.Uri Source
		{
			get => new global::System.Uri(this.GetHtmlAttribute("src"));
			set
			{
                //var path = Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_APP_BASE");
                //var target = $"/{path}/MonacoCodeEditor.html";
                //var target = (value.IsAbsoluteUri && value.IsFile)
                //	? value.PathAndQuery 
                //	: value.ToString();

                string target;
				if (value.IsAbsoluteUri)
				{
					if(value.Scheme=="file")
					{
						// Local files are assumed as coming from the remoter server
						target = UNO_BOOTSTRAP_APP_BASE == null ? value.PathAndQuery : UNO_BOOTSTRAP_WEBAPP_BASE_PATH + UNO_BOOTSTRAP_APP_BASE + value.PathAndQuery;
					}
                    else
                    {
						target = value.AbsoluteUri;

					}

				}
				else
				{
					target = UNO_BOOTSTRAP_APP_BASE == null
						? value.OriginalString
						: UNO_BOOTSTRAP_WEBAPP_BASE_PATH + UNO_BOOTSTRAP_APP_BASE + "/" + value.OriginalString;
				}

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
				{
					this.Log().Debug($"Loading {target} (Nav is null {NavigationStarting == null})");
				}

				this.SetHtmlAttribute("src", target);

				//NavigationStarting?.Invoke(this, new WebViewNavigationStartingEventArgs());
				Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => NavigationStarting?.Invoke(this, new WebViewNavigationStartingEventArgs()));
			}
		}

		/// <inheritdoc />
		public IAsyncOperation<string> InvokeScriptAsync(string scriptName, IEnumerable<string> arguments)
		{
			var script = $@"(function() {{
				try {{
					window.__evalMethod = function() {{ {arguments.Single()} }};
					
					return window.eval(""__evalMethod()"") || """";
				}}
				catch(err){{
					Debug.log(err);
				}}
				finally {{
					window.__evalMethod = null;
				}}
			}})()";

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
			{
				this.Log().Debug("Invoke Script: " + script);
			}

			try
			{
				var result = this.ExecuteJavascript(script);

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
				{
					this.Log().Debug($"Invoke Script result: {result}");
				}

				return Task.FromResult(result).AsAsyncOperation();
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
				{
					this.Log().Error("Invoke Script failed", e);
				}

				return Task.FromResult("").AsAsyncOperation();
			}
		}

		public void Launch()
		{
			string javascript = $@"createMonacoEditor({Handle}, '{UNO_BOOTSTRAP_WEBAPP_BASE_PATH}{UNO_BOOTSTRAP_APP_BASE}', element)";

            this.ExecuteJavascript(javascript);
        }
	}
}
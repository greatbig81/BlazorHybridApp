using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;
#if ANDROID
using Microsoft.Maui.Platform;
using Android.Webkit;
using BlazorHybridApp.Services;
#endif


namespace BlazorHybridApp
{
    public static class MauiProgram
    {

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                })
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    handlers.AddHandler<BlazorWebView, CustomBlazorWebViewHandler>();
#endif
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

#if ANDROID
            // ObjectRecognitive 싱글톤 등록
            builder.Services.AddSingleton<ObjectRecognitive>();
#endif

            return builder.Build();
        }

#if ANDROID
        // 커스텀 핸들러 정의
        public class CustomBlazorWebViewHandler : BlazorWebViewHandler
        {
            protected override Android.Webkit.WebView CreatePlatformView()
            {
                var webView = base.CreatePlatformView();

                // 커스텀 WebChromeClient 설정
                webView.SetWebChromeClient(new CustomWebChromeClient());

                return webView;
            }
        }

        // 미디어 권한 요청을 처리하는 WebChromeClient
        public class CustomWebChromeClient : WebChromeClient
        {
            public override void OnPermissionRequest(PermissionRequest request)
            {
                // 카메라 권한 요청을 받으면 무조건 승인합니다.
                // 이미 네이티브 권한이 Granted 상태이므로 안전합니다.
                if (request.GetResources().Any(r => r == PermissionRequest.ResourceVideoCapture))
                {
                    request.Grant(new string[] { PermissionRequest.ResourceVideoCapture });
                    return;
                }

                // 다른 권한 요청은 기본 처리
                base.OnPermissionRequest(request);
            }
        }
#endif
    }
}

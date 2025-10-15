using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
//using Microsoft.Maui.Graphics.Platform;
using IImage = Microsoft.Maui.Graphics.IImage;

#if ANDROID
using Android.Graphics;
using Microsoft.Maui.Graphics.Platform;
#elif IOS || MACCATALYST
using UIKit;
using Microsoft.Maui.Graphics.Platform;
#elif WINDOWS
using Microsoft.Maui.Graphics.Win2D;
#endif


namespace BlazorHybridApp.Services
{
    /// <summary>
    /// 카메라 영상 객체 인식 클래스 (MAUI Graphics 사용)
    /// </summary>
    public class ObjectRecognitive : IDisposable
    {
        private const string API_URL = "http://61.41.4.9:3031/api/image-cognitive";
        private const int TARGET_WIDTH = 640;
        private const int TARGET_HEIGHT = 640;
        private const int CAPTURE_INTERVAL = 1000; // 1초

        private readonly HttpClient httpClient;
        private Timer captureTimer;
        private bool isRunning;
        private readonly object lockObject = new object();

        // 원본 이미지 크기 저장
        private SizeF originalImageSize;
        private SizeF scaledImageSize;

        /// <summary>
        /// 객체 인식 결과 수신 이벤트
        /// </summary>
        public event EventHandler<RecognitionResult> OnRecognitionCompleted;

        /// <summary>
        /// 오류 발생 이벤트
        /// </summary>
        public event EventHandler<Exception> OnError;

        /// <summary>
        /// 이미지 캡처 이벤트 (원본 이미지 제공)
        /// </summary>
        public event EventHandler<IImage> OnImageCaptured;

        /// <summary>
        /// 실행 상태
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (lockObject)
                {
                    return isRunning;
                }
            }
            private set
            {
                lock (lockObject)
                {
                    isRunning = value;
                }
            }
        }

        /// <summary>
        /// 캡처 간격 (밀리초)
        /// </summary>
        public int CaptureInterval { get; set; } = CAPTURE_INTERVAL;

        /// <summary>
        /// API 타임아웃 (밀리초)
        /// </summary>
        public int ApiTimeout { get; set; } = 10000;

        /// <summary>
        /// JPEG 품질 (0-100)
        /// </summary>
        public int JpegQuality { get; set; } = 85;

        /// <summary>
        /// 생성자
        /// </summary>
        public ObjectRecognitive()
        {
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(ApiTimeout)
            };
            scaledImageSize = new SizeF(TARGET_WIDTH, TARGET_HEIGHT);
        }

        /// <summary>
        /// 객체 인식 시작
        /// </summary>
        public void Start()
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("이미 실행 중입니다.");
            }

            IsRunning = true;
            captureTimer = new Timer(CaptureCallback, null, 0, CaptureInterval);
            Console.WriteLine("객체 인식 시작됨");
        }

        /// <summary>
        /// 객체 인식 중지
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            captureTimer?.Dispose();
            captureTimer = null;
            Console.WriteLine("객체 인식 중지됨");
        }

        /// <summary>
        /// 타이머 콜백 - 주기적으로 카메라 이미지 캡처 및 인식 수행
        /// </summary>
        private async void CaptureCallback(object state)
        {
            if (!IsRunning)
                return;

            try
            {
                // 카메라에서 이미지 캡처
                IImage originalImage = await CaptureFromCamera();

                if (originalImage == null)
                {
                    Console.WriteLine("카메라 이미지 캡처 실패");
                    return;
                }

                // 원본 이미지 크기 저장
                originalImageSize = new SizeF(originalImage.Width, originalImage.Height);
                Console.WriteLine($"원본 이미지 크기: {originalImageSize.Width}x{originalImageSize.Height}");

                // 이미지 캡처 이벤트 발생
                OnImageCaptured?.Invoke(this, originalImage);

                // 이미지 리스케일 및 JPG 변환
                byte[] imageData = await ResizeAndConvertToJpeg(originalImage, TARGET_WIDTH, TARGET_HEIGHT);

                if (imageData == null || imageData.Length == 0)
                {
                    Console.WriteLine("이미지 변환 실패");
                    return;
                }

                Console.WriteLine($"JPG 데이터 크기: {imageData.Length} bytes");

                // API 요청
                var apiResponse = await SendToRecognitionAPI(imageData);

                if (apiResponse != null)
                {
                    // 좌표 변환 (스케일된 이미지 → 원본 이미지)
                    ConvertCoordinatesToOriginal(apiResponse);

                    // 인식 결과 이벤트 발생
                    OnRecognitionCompleted?.Invoke(this, apiResponse);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"객체 인식 오류: {ex.Message}");
                OnError?.Invoke(this, ex);
            }
        }

        /// <summary>
        /// 카메라에서 이미지 캡처 (플랫폼별 구현)
        /// </summary>
        private async Task<IImage> CaptureFromCamera()
        {
            try
            {
#if ANDROID || IOS || MACCATALYST
                // MediaPicker를 사용한 카메라 캡처
                var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = "카메라 캡처"
                });

                if (photo != null)
                {
                    using var stream = await photo.OpenReadAsync();
                    return PlatformImage.FromStream(stream);
                }
#elif WINDOWS
                // Windows에서는 CameraCaptureUI 또는 MediaCapture 사용
                // 여기서는 테스트용 더미 이미지 반환
                return CreateTestImage();
#else
                return CreateTestImage();
#endif
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"카메라 캡처 오류: {ex.Message}");
                return CreateTestImage(); // 오류 시 테스트 이미지 반환
            }
        }

        /// <summary>
        /// 테스트용 더미 이미지 생성
        /// </summary>
        private IImage CreateTestImage()
        {
            try
            {
                int width = 1920;
                int height = 1080;

#if WINDOWS
                var image = new W2DImageLoadingService().FromStream(CreateTestImageStream(width, height));
                return image;
#else
                return PlatformImage.FromStream(CreateTestImageStream(width, height));
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"테스트 이미지 생성 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 테스트 이미지 스트림 생성
        /// </summary>
        private Stream CreateTestImageStream(int width, int height)
        {
            var stream = new MemoryStream();

            /*
            // MAUI Graphics를 사용하여 이미지 생성
            using (var canvas = new PdfCanvas(width, height))
            {
                canvas.FillColor = Colors.LightGray;
                canvas.FillRectangle(0, 0, width, height);

                canvas.FontColor = Colors.Black;
                canvas.FontSize = 24;
                canvas.DrawString("Test Image", 10, 30, HorizontalAlignment.Left);

                canvas.DrawString($"{width}x{height}", 10, 60, HorizontalAlignment.Left);
            }
            */

            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// 이미지 리스케일 및 JPEG 변환
        /// </summary>
        private async Task<byte[]> ResizeAndConvertToJpeg(IImage originalImage, int targetWidth, int targetHeight)
        {
            return await Task.Run(() =>
                {
                    try
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            // 리스케일된 이미지를 JPEG로 저장
                            originalImage.Resize(targetWidth, targetHeight, ResizeMode.Fit, true);
                            originalImage.Save(memoryStream, Microsoft.Maui.Graphics.ImageFormat.Jpeg, (float)JpegQuality / 100f);

                            return memoryStream.ToArray();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"이미지 리스케일/변환 오류: {ex.Message}");

                        // 대체 방법: 수동으로 리스케일
                        try
                        {
                            return ResizeImageManually(originalImage, targetWidth, targetHeight);
                        }
                        catch (Exception ex2)
                        {
                            Console.WriteLine($"수동 리스케일 오류: {ex2.Message}");
                            return null;
                        }
                    }
                }
                );
        }

        /// <summary>
        /// 수동 이미지 리스케일 (Graphics를 사용)
        /// </summary>
        private byte[] ResizeImageManually(IImage originalImage, int targetWidth, int targetHeight)
        {
            // 스케일 비율 계산 (Fit 모드)
            float scaleX = (float)targetWidth / originalImage.Width;
            float scaleY = (float)targetHeight / originalImage.Height;
            float scale = Math.Min(scaleX, scaleY);

            int newWidth = (int)(originalImage.Width * scale);
            int newHeight = (int)(originalImage.Height * scale);

            using (var memoryStream = new MemoryStream())
            {
#if ANDROID
                // Android 플랫폼 구현
                using (var bitmap = Android.Graphics.Bitmap.CreateScaledBitmap(
                    ((PlatformImage)originalImage).AsBitmap(),
                    newWidth,
                    newHeight,
                    true))
                {
                    bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, JpegQuality, memoryStream);
                }
#elif IOS || MACCATALYST
                /*
                // iOS 플랫폼 구현
                var uiImage = ((PlatformImage)originalImage) as UIImage;
                UIGraphics.BeginImageContext(new CoreGraphics.CGSize(newWidth, newHeight));
                uiImage.Draw(new CoreGraphics.CGRect(0, 0, newWidth, newHeight));
                var resizedImage = UIGraphics.GetImageFromCurrentImageContext();
                UIGraphics.EndImageContext();
                
                var data = resizedImage.AsJPEG((nfloat)(JpegQuality / 100.0));
                data.AsStream().CopyTo(memoryStream);
                */
#elif WINDOWS
                // Windows 플랫폼 구현
                originalImage.Save(memoryStream, ImageFormat.Jpeg, (float)JpegQuality / 100f);
#endif
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// 인식 API에 이미지 전송
        /// </summary>
        private async Task<RecognitionResult> SendToRecognitionAPI(byte[] imageData)
        {
            try
            {
                using (var content = new ByteArrayContent(imageData))
                {
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                    Console.WriteLine($"API 요청 전송: {API_URL} (크기: {imageData.Length} bytes)");

                    HttpResponseMessage response = await httpClient.PostAsync(API_URL, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"API 응답 수신: {jsonResponse.Length} chars");

                        // JSON 파싱
                        var result = ParseRecognitionResponse(jsonResponse);
                        return result;
                    }
                    else
                    {
                        Console.WriteLine($"API 오류: {response.StatusCode}");
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"오류 내용: {errorContent}");
                        return null;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"네트워크 오류: {ex.Message}");
                OnError?.Invoke(this, ex);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"요청 타임아웃: {ex.Message}");
                OnError?.Invoke(this, ex);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API 요청 오류: {ex.Message}");
                OnError?.Invoke(this, ex);
                return null;
            }
        }

        /// <summary>
        /// JSON 응답 파싱
        /// </summary>
        private RecognitionResult ParseRecognitionResponse(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiObjects = JsonSerializer.Deserialize<List<RecognizedObject>>(json, options);

                return new RecognitionResult
                {
                    Timestamp = DateTime.Now,
                    Objects = apiObjects ?? new List<RecognizedObject>(),
                    OriginalImageSize = originalImageSize,
                    ScaledImageSize = scaledImageSize
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JSON 파싱 오류: {ex.Message}");
                Console.WriteLine($"JSON 내용: {json}");
                throw;
            }
        }

        /// <summary>
        /// 좌표를 원본 이미지 크기로 변환
        /// </summary>
        private void ConvertCoordinatesToOriginal(RecognitionResult result)
        {
            if (result.Objects == null || result.Objects.Count == 0)
                return;

            // 스케일 비율 계산
            float scaleX = originalImageSize.Width / scaledImageSize.Width;
            float scaleY = originalImageSize.Height / scaledImageSize.Height;

            Console.WriteLine($"좌표 변환: Scale X={scaleX:F2}, Y={scaleY:F2}");

            foreach (var obj in result.Objects)
            {
                if (obj.BoundingBox != null)
                {
                    // 스케일된 좌표를 원본 좌표로 변환
                    obj.OriginalBoundingBox = new BoundingBox
                    {
                        X = (int)(obj.BoundingBox.X * scaleX),
                        Y = (int)(obj.BoundingBox.Y * scaleY),
                        Width = (int)(obj.BoundingBox.Width * scaleX),
                        Height = (int)(obj.BoundingBox.Height * scaleY)
                    };

                    Console.WriteLine($"객체 '{obj.Name}': 스케일({obj.BoundingBox}) → 원본({obj.OriginalBoundingBox})");
                }
            }
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            Stop();
            httpClient?.Dispose();
            captureTimer?.Dispose();
        }
    }

    /// <summary>
    /// 인식 결과 클래스
    /// </summary>
    public class RecognitionResult
    {
        public DateTime Timestamp { get; set; }
        public List<RecognizedObject> Objects { get; set; }
        public SizeF OriginalImageSize { get; set; }
        public SizeF ScaledImageSize { get; set; }
        public SizeF RenderedImageSize { get; set; }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] 감지된 객체: {Objects?.Count ?? 0}개";
        }
    }

    /// <summary>
    /// 인식된 객체 클래스
    /// </summary>
    public class RecognizedObject
    {
        /// <summary>
        /// 객체 이름
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 신뢰도 (0.0 ~ 1.0)
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// 바운딩 박스 (스케일된 이미지 기준)
        /// </summary>
        public BoundingBox BoundingBox { get; set; }

        /// <summary>
        /// 바운딩 박스 (원본 이미지 기준)
        /// </summary>
        public BoundingBox OriginalBoundingBox { get; set; }

        public BoundingBox RenderBoundingBox { get; set; }

        public override string ToString()
        {
            return $"{Name} ({Confidence:P1}) at {OriginalBoundingBox}";
        }
    }

    /// <summary>
    /// 바운딩 박스 (위치 정보)
    /// </summary>
    public class BoundingBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public override string ToString()
        {
            return $"[{X}, {Y}, {Width}x{Height}]";
        }
    }

}

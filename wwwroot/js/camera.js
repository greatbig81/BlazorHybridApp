
let currentStream = null;

// startCameraFeed 함수 정의
window.startCameraFeed = function (videoId, facingMode) {
    const video = document.getElementById(videoId);

    // 1. 기존 스트림이 있다면 중지 (토글 시 필요)
    if (currentStream) {
        currentStream.getTracks().forEach(track => track.stop());
    }

    // 2. 새로운 제약 조건 설정
    const constraints = {
        video: {
            facingMode: facingMode
        }
    };

    if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
        // 카메라와 마이크 접근 요청
        navigator.mediaDevices.getUserMedia(constraints)
            .then(function (stream) {
                currentStream = stream; // 현재 스트림 저장
                // 스트림을 video 엘리먼트의 source로 설정하여 영상 렌더링 시작
                video.srcObject = stream;
                video.play();
            })
            .catch(function (error) {
                // 사용자가 권한을 거부했거나 카메라가 없을 때의 처리
                console.error("Camera Error: ", error);
                alert("Camera Error - Camera access was denied or the camera could not be found. error:" + error);
            });
    } else {
        alert("Camera Error - This browser does not support the MediaDevices API.");
    }
};

// MAUI Blazor Hybrid 환경에서는 .js 파일을 index.html에 로드해야 합니다.


window.CameraHelper = {
    videoElement: null,
    streamActive: false,
    dotnetReference: null,
    target_width: 640,
    target_height: 640,

    // 카메라 초기화
    async initialize(videoElementId, facingMode) {
        try {
            this.videoElement = document.getElementById(videoElementId);

            if (!this.videoElement) {
                console.error('Video element not found:', videoElementId);
                return false;
            }

            // 카메라 스트림 요청
            const stream = await navigator.mediaDevices.getUserMedia({
                video: {
                    width: { ideal: 1920 },
                    height: { ideal: 1080 },
                    facingMode: facingMode // 'environment' // 또는 'user' (전면 카메라)
                },
                audio: false
            });

            this.videoElement.srcObject = stream;
            this.videoElement.play();
            this.streamActive = true;

            console.log('Camera initialized successfully');
            return true;
        } catch (error) {
            console.error('Camera initialization error:', error);
            return false;
        }
    },

    // 카메라 중지
    stop() {
        if (this.videoElement && this.videoElement.srcObject) {
            const tracks = this.videoElement.srcObject.getTracks();
            tracks.forEach(track => track.stop());
            this.videoElement.srcObject = null;
            this.streamActive = false;
            console.log('Camera stopped');
        }
    },

    // 현재 프레임을 Base64로 캡처
    captureFrameAsBase64(quality = 0.85) {
        if (!this.videoElement || !this.streamActive) {
            console.error('Video element not ready or stream not active');
            return null;
        }

        try {
            const canvas = document.createElement('canvas');
            canvas.width = this.target_width;    // this.videoElement.videoWidth;
            canvas.height = this.target_height;  // this.videoElement.videoHeight;

            const context = canvas.getContext('2d');
            context.drawImage(this.videoElement, 0, 0, canvas.width, canvas.height);

            // JPEG로 변환 (data:image/jpeg;base64,... 형식)
            const base64Data = canvas.toDataURL('image/jpeg', quality);

            // "data:image/jpeg;base64," 부분 제거
            const base64String = base64Data.split(',')[1];

            console.log('Frame captured:', canvas.width, 'x', canvas.height,
                'Size:', base64String.length, 'chars');

            return base64String;
        } catch (error) {
            console.error('Frame capture error:', error);
            return null;
        }
    },

    // 현재 프레임을 Blob으로 캡처
    async captureFrameAsBlob(quality = 0.85) {
        if (!this.videoElement || !this.streamActive) {
            console.error('Video element not ready or stream not active');
            return null;
        }

        try {
            const canvas = document.createElement('canvas');
            canvas.width = this.videoElement.videoWidth;
            canvas.height = this.videoElement.videoHeight;

            const context = canvas.getContext('2d');
            context.drawImage(this.videoElement, 0, 0, canvas.width, canvas.height);

            return new Promise((resolve, reject) => {
                canvas.toBlob(
                    (blob) => {
                        if (blob) {
                            console.log('Frame captured as blob:', blob.size, 'bytes');
                            resolve(blob);
                        } else {
                            reject(new Error('Blob creation failed'));
                        }
                    },
                    'image/jpeg',
                    quality
                );
            });
        } catch (error) {
            console.error('Frame capture error:', error);
            return null;
        }
    },

    // Blob을 Base64로 변환
    async blobToBase64(blob) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onloadend = () => {
                const base64String = reader.result.split(',')[1];
                resolve(base64String);
            };
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });
    },

    // 비디오 정보 가져오기
    getVideoInfo() {
        if (!this.videoElement || !this.streamActive) {
            return null;
        }

        return {
            width: this.videoElement.videoWidth,
            height: this.videoElement.videoHeight,
            readyState: this.videoElement.readyState,
            paused: this.videoElement.paused,
            renderedWidth: this.videoElement.offsetWidth,
            renderedHeight: this.videoElement.offsetHeight
        };
    },

    // .NET 참조 설정 (콜백용)
    setDotNetReference(dotnetRef) {
        this.dotnetReference = dotnetRef;
    },

    // 자동 캡처 시작 (주기적으로 프레임을 C#으로 전송)
    startAutoCapture(intervalMs = 1000) {
        if (this.autoCaptureInterval) {
            clearInterval(this.autoCaptureInterval);
        }

        this.autoCaptureInterval = setInterval(async () => {
            if (this.dotnetReference && this.streamActive) {
                const base64Data = this.captureFrameAsBase64();
                if (base64Data) {
                    await this.dotnetReference.invokeMethodAsync('OnFrameCaptured', base64Data);
                }
            }
        }, intervalMs);

        console.log('Auto capture started with interval:', intervalMs, 'ms');
    },

    // 자동 캡처 중지
    stopAutoCapture() {
        if (this.autoCaptureInterval) {
            clearInterval(this.autoCaptureInterval);
            this.autoCaptureInterval = null;
            console.log('Auto capture stopped');
        }
    }
};



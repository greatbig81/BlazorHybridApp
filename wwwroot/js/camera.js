
let currentStream = null;

// startCameraFeed �Լ� ����
window.startCameraFeed = function (videoId, facingMode) {
    const video = document.getElementById(videoId);

    // 1. ���� ��Ʈ���� �ִٸ� ���� (��� �� �ʿ�)
    if (currentStream) {
        currentStream.getTracks().forEach(track => track.stop());
    }

    // 2. ���ο� ���� ���� ����
    const constraints = {
        video: {
            facingMode: facingMode
        }
    };

    if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
        // ī�޶�� ����ũ ���� ��û
        navigator.mediaDevices.getUserMedia(constraints)
            .then(function (stream) {
                currentStream = stream; // ���� ��Ʈ�� ����
                // ��Ʈ���� video ������Ʈ�� source�� �����Ͽ� ���� ������ ����
                video.srcObject = stream;
                video.play();
            })
            .catch(function (error) {
                // ����ڰ� ������ �ź��߰ų� ī�޶� ���� ���� ó��
                console.error("Camera Error: ", error);
                alert("Camera Error - Camera access was denied or the camera could not be found. error:" + error);
            });
    } else {
        alert("Camera Error - This browser does not support the MediaDevices API.");
    }
};

// MAUI Blazor Hybrid ȯ�濡���� .js ������ index.html�� �ε��ؾ� �մϴ�.


window.CameraHelper = {
    videoElement: null,
    streamActive: false,
    dotnetReference: null,
    target_width: 640,
    target_height: 640,

    // ī�޶� �ʱ�ȭ
    async initialize(videoElementId, facingMode) {
        try {
            this.videoElement = document.getElementById(videoElementId);

            if (!this.videoElement) {
                console.error('Video element not found:', videoElementId);
                return false;
            }

            // ī�޶� ��Ʈ�� ��û
            const stream = await navigator.mediaDevices.getUserMedia({
                video: {
                    width: { ideal: 1920 },
                    height: { ideal: 1080 },
                    facingMode: facingMode // 'environment' // �Ǵ� 'user' (���� ī�޶�)
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

    // ī�޶� ����
    stop() {
        if (this.videoElement && this.videoElement.srcObject) {
            const tracks = this.videoElement.srcObject.getTracks();
            tracks.forEach(track => track.stop());
            this.videoElement.srcObject = null;
            this.streamActive = false;
            console.log('Camera stopped');
        }
    },

    // ���� �������� Base64�� ĸó
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

            // JPEG�� ��ȯ (data:image/jpeg;base64,... ����)
            const base64Data = canvas.toDataURL('image/jpeg', quality);

            // "data:image/jpeg;base64," �κ� ����
            const base64String = base64Data.split(',')[1];

            console.log('Frame captured:', canvas.width, 'x', canvas.height,
                'Size:', base64String.length, 'chars');

            return base64String;
        } catch (error) {
            console.error('Frame capture error:', error);
            return null;
        }
    },

    // ���� �������� Blob���� ĸó
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

    // Blob�� Base64�� ��ȯ
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

    // ���� ���� ��������
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

    // .NET ���� ���� (�ݹ��)
    setDotNetReference(dotnetRef) {
        this.dotnetReference = dotnetRef;
    },

    // �ڵ� ĸó ���� (�ֱ������� �������� C#���� ����)
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

    // �ڵ� ĸó ����
    stopAutoCapture() {
        if (this.autoCaptureInterval) {
            clearInterval(this.autoCaptureInterval);
            this.autoCaptureInterval = null;
            console.log('Auto capture stopped');
        }
    }
};



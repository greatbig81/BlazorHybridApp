
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
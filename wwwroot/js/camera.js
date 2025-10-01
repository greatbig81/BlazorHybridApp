// startCameraFeed �Լ� ����
window.startCameraFeed = function (videoId) {
    const video = document.getElementById(videoId);

    if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
        // ī�޶�� ����ũ ���� ��û
        navigator.mediaDevices.getUserMedia({ video: true })
            .then(function (stream) {
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
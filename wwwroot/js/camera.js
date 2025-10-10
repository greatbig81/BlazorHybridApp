
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
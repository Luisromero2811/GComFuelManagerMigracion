function timerInactivo(dotnetHelper) {
    var timer;
    document.onmousemove = resetTimer;
    document.onkeypress = resetTimer;

    function resetTimer() {
        clearTimeout(timer);
        timer = setTimeout(logout, 4 * 1000 * 60);
    }
    function logout() {
        dotnetHelper.invokeMethodAsync("Logoute");
    }
}

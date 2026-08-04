# Guncelleme Yayinlama Asamalari

TurcaDesk ile ayni yontem: ClickOnce, Any CPU, framework-dependent (RuntimeIdentifier
YOK). Ayri x86/x64 publish gerekmez — SkiaSharp gibi native paketler runtimes\win-x86,
runtimes\win-x64, runtimes\win-arm64 klasorlerinin hepsini tek cikti icine koyar, .NET
calisirken hangi mimaride oldugunu kendisi anlayip dogru olani yukler.

FTP giris bilgileri: mevcut update.turca.app hesabinizla ayni (bkz. TurcaDesk yayinlama
notlari), sadece hedef klasor bu sefer TurcaExce. Giris bilgilerini bu dosyaya yazmayin.

Versiyon yayinlarken TurcaExce icinde AssemblyVersion guncelle
--------------------------------------------------------------------
00 - Publish -> Settings -> ApplicationVersion / Publish Version numarasini kontrol et
01 - TurcaExce.csproj -> AssemblyVersion -> versiyon numarasi ayni olacak
02 - Visual Studio'da sag tik -> Publish -> ClickOnceProfile ile yayinla
     (veya: msbuild TurcaExce.csproj /t:Publish /p:PublishProfile=ClickOnceProfile.pubxml /p:Configuration=Release)
03 - TurcaExce\bin\Publish icerisinde yuklenecek dosya yapisi olusur
     (Application Files klasoru, TurcaExce.application, setup.exe)
04 - Bu dosyalari public_html/update.turca.app/TurcaExce/Download icerisine at
05 - public_html/update.turca.app/TurcaExce -> version.json bilgisini guncelle

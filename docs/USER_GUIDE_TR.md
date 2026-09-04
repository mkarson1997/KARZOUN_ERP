# Kullanıcı Kılavuzu: Belge Metinlerinin Yerelleştirilmesi (v1.0.2)

Bu kılavuz, **KARZOUN ERP** uygulamasında dil bazlı şirket belge metinlerinin (fatura, fiyat teklifi vb.) nasıl kullanılacağını açıklamaktadır.

## Dil Bazlı Şirket Metinlerinin Ayarlanması
v1.0.2 sürümünden itibaren fatura notları, fiyat teklifi notları, yasal altbilgi, varsayılan ödeme bilgileri ve QR kod şablon metinlerini her dil (Arapça, Türkçe, İngilizce) için ayrı ayrı ayarlayabilirsiniz.

### Metinleri Ayarlama Adımları:
1. **Ayarlar** sayfasına gidin.
2. Metinlerini ayarlamak istediğiniz dili seçin (Örn: Türkçe).
3. Dili uygulamak için **"Dili Kaydet"** düğmesine tıklayın.
4. **"Varsayılan Metinler"** bölümüne gidin ve seçilen dil için alanları doldurun:
   - Varsayılan Fatura Notları
   - Varsayılan Fiyat Teklifi Notları
   - Yasal Altbilgi Metni
   - Varsayılan Ödeme Bilgileri
   - QR Kodu Metni / URL Şablonu
5. **"Ayarları Kaydet"** düğmesine tıklayın.
6. Arapça ve İngilizce metinleri ayarlamak için dili seçip kaydedin, ardından ilgili metin alanlarını düzenleyip Ayarları Kaydedin.

## Belge Oluştururken Metinlerin Kullanımı
- Yeni bir fiyat teklifi veya fatura oluşturulduğunda sistem, seçilen belge diline göre şirket ayarlarına kaydedilmiş notları ve ödeme bilgilerini otomatik olarak yükler.
- Belge kaydedilmeden önce belge dilini değiştirirseniz, sistem varsayılan metinleri otomatik olarak yeni belge diline göre günceller.
- Kaydedilmiş eski belgeler bu durumdan etkilenmez, orijinal notları korunur.

## PDF Dışa Aktarma
- Fatura veya fiyat teklifi PDF olarak dışa aktarılırken, dışa aktarılan dile ait şirket metinleri yüklenir.
- Eğer belge varsayılan Arapça notlara sahipse ve PDF Türkçe ya da İngilizce olarak dışa aktarılıyorsa, sistem yabancı dildeki belgelerde Arapça varsayılan metinlerin görünmesini önlemek için bunları otomatik olarak hedef dilin notlarıyla değiştirir.

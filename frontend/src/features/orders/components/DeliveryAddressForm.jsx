export function DeliveryAddressForm({ value, onChange, busy }) {
  const field = (name) => ({
    value: value[name],
    disabled: busy,
    onChange: (event) =>
      onChange((current) => ({ ...current, [name]: event.target.value })),
  });
  return (
    <fieldset className="delivery-fields" disabled={busy}>
      <legend>Çatdırılma məlumatları</legend>
      <label>
        Alıcının adı
        <input
          {...field("recipientName")}
          required
          maxLength={150}
          autoComplete="name"
        />
      </label>
      <label>
        Telefon nömrəsi
        <input
          {...field("deliveryPhone")}
          required
          type="tel"
          maxLength={30}
          autoComplete="tel"
          placeholder="+994 50 123 45 67"
        />
      </label>
      <label>
        Şəhər / rayon
        <input
          {...field("deliveryCity")}
          required
          maxLength={100}
          autoComplete="address-level2"
          placeholder="Bakı"
        />
      </label>
      <label>
        Tam ünvan
        <textarea
          {...field("deliveryAddress")}
          required
          maxLength={500}
          autoComplete="street-address"
          rows={3}
          placeholder="Küçə, bina/ev, mənzil"
        />
      </label>
      <label>
        Çatdırılma qeydi <span className="meta">(istəyə bağlı)</span>
        <textarea
          {...field("deliveryNote")}
          maxLength={500}
          rows={2}
          placeholder="Mərtəbə, giriş və ya kuryer üçün qeyd"
        />
      </label>
    </fieldset>
  );
}

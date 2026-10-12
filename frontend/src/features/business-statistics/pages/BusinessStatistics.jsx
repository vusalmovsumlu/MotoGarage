import { useState } from "react";
import { Page } from "../../../components/layout/Page.jsx";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { Empty } from "../../../components/feedback/Empty.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import { useResource } from "../../../shared/hooks/useResource.js";
import { useApp } from "../../../shared/hooks/useApp.js";
import { money, number } from "../../../shared/utils/format.js";
import { bakuDay, shiftDay } from "../../../shared/utils/bakuTime.js";
import { statisticsApi } from "../api/statisticsApi.js";

export function BusinessStatistics() {
  const today = bakuDay();
  const [period, setPeriod] = useState({
    from: shiftDay(today, -29),
    to: today,
  });
  const [form, setForm] = useState(period);
  const [error, setError] = useState("");
  const { user } = useApp();
  const state = useResource(statisticsApi.path(period.from, period.to));
  const mechanic = user.role === "Mechanic";
  const data = state.data;
  const metrics = mechanic
    ? [
        ["Tamamlanmış görüşlər", number(data?.completedCount || 0)],
        ["Təsdiqlənmiş servis məbləği", money(data?.amount || 0)],
        ["Ümumi orta reytinq", `${number(data?.averageRating || 0)} / 5`],
        ["Rəylərin sayı", number(data?.reviewCount || 0)],
      ]
    : [
        ["Tamamlanmış satış məbləği", money(data?.amount || 0)],
        ["Tamamlanmış sifarişlər", number(data?.completedCount || 0)],
        ["Ləğv edilmiş sifarişlər", number(data?.cancelledCount || 0)],
        ["Aktiv sifarişlər", number(data?.pendingCount || 0)],
      ];
  return (
    <Page
      title="Biznes statistikası"
      description={
        mechanic
          ? "Görülən işləriniz və müştərilərin qiymətləndirmələri."
          : "Satışlarınız, sifarişləriniz və stok vəziyyətiniz."
      }
    >
      <form
        className="planning-toolbar"
        onSubmit={(event) => {
          event.preventDefault();
          if (
            form.to < form.from ||
            (new Date(form.to) - new Date(form.from)) / 86400000 >= 366
          ) {
            setError("Dövr düzgün olmalı və 366 günü keçməməlidir.");
            return;
          }
          setError("");
          setPeriod(form);
        }}
      >
        <label>
          Başlanğıc
          <input
            type="date"
            required
            value={form.from}
            onChange={(event) => setForm({ ...form, from: event.target.value })}
          />
        </label>
        <label>
          Son gün
          <input
            type="date"
            required
            min={form.from}
            value={form.to}
            onChange={(event) => setForm({ ...form, to: event.target.value })}
          />
        </label>
        <Button type="submit">Göstər</Button>
      </form>
      {error && (
        <p role="alert" className="form-error">
          {error}
        </p>
      )}
      <Resource state={state}>
        <div className="business-metrics">
          {metrics.map(([label, value]) => (
            <article className="card" key={label}>
              <p className="meta">{label}</p>
              <strong>{value}</strong>
            </article>
          ))}
        </div>
        <p className="meta">
          Seçilən dövr: {period.from} — {period.to} (Bakı vaxtı).{" "}
          {mechanic
            ? "Məbləğ servis tarixinə görə hesablanır. Reytinq və rəy sayı bütün dövrə aiddir."
            : "Satışlar tamamlanma, ləğvlər ləğv tarixinə görə hesablanır. Aktiv sifariş və stok sayları hazırkı vəziyyətdir."}
        </p>
        {mechanic ? (
          <div className="card">
            <h2>Hazırda aktiv görüşlər</h2>
            <p>{number(data?.pendingCount || 0)} görüş</p>
            <a href="#/work/calendar" className="button secondary">
              Təqvimə bax
            </a>
          </div>
        ) : (
          <>
            <div className="business-metrics stock-summary">
              <a className="card" href="#/inventory?stock=low">
                <p className="meta">Az stoklu məhsullar</p>
                <strong>{number(data?.lowStockCount || 0)}</strong>
              </a>
              <a className="card" href="#/inventory?stock=out">
                <p className="meta">Stoku bitən məhsullar</p>
                <strong>{number(data?.outOfStockCount || 0)}</strong>
              </a>
            </div>
            <section className="card">
              <h2>Ən çox satılan məhsullar</h2>
              {data?.bestSellers?.length ? (
                <div className="table-scroll">
                  <table className="business-table">
                    <thead>
                      <tr>
                        <th>Məhsul</th>
                        <th>Satılan say</th>
                        <th>Məbləğ</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.bestSellers.map((product) => (
                        <tr key={product.productId}>
                          <td>{product.name}</td>
                          <td>{number(product.quantity)}</td>
                          <td>{money(product.amount)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <Empty title="Bu dövrdə tamamlanmış satış yoxdur" />
              )}
            </section>
          </>
        )}
      </Resource>
    </Page>
  );
}

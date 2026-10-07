import "./Skeleton.css";

function Block({ width = "100%", height = 14, className = "" }) {
  return (
    <span className={`skeleton-block ${className}`} style={{ width, height }} />
  );
}

function Rows({ compact = false }) {
  return (
    <div
      className={
        compact ? "skeleton-rows" : "card skeleton-panel skeleton-rows"
      }
    >
      {[0, 1, 2].map((index) => (
        <div className="skeleton-row" key={index}>
          <Block width={42} height={42} className="skeleton-avatar" />
          <div className="skeleton-stack">
            <Block width={index === 1 ? "65%" : "80%"} />
            <Block width="45%" height={10} />
          </div>
          <Block width={48} height={20} className="skeleton-pill" />
        </div>
      ))}
    </div>
  );
}

function Stats({ count = 4 }) {
  return (
    <div className="stats-grid">
      {Array.from({ length: count }, (_, index) => (
        <div className="card stat skeleton-stack" key={index}>
          <Block width="60%" height={12} />
          <Block width={72} height={30} />
          <Block width="45%" height={10} />
        </div>
      ))}
    </div>
  );
}

function Dashboard() {
  return (
    <>
      <Stats />
      <div className="workspace-columns">
        <aside className="summary-column">
          {[0, 1, 2].map((index) => (
            <div className="card skeleton-panel skeleton-stack" key={index}>
              <Block width="65%" />
              <Block width="90%" height={12} />
              <Block width="55%" height={10} />
            </div>
          ))}
        </aside>
        <div className="destinations-column">
          <Block width="40%" />
          <div className="app-shortcuts">
            {[0, 1, 2, 3, 4, 5].map((index) => (
              <div className="skeleton-shortcut skeleton-stack" key={index}>
                <Block width={78} height={78} className="skeleton-art" />
                <Block width="55%" height={12} />
                <Block width="75%" height={10} />
              </div>
            ))}
          </div>
        </div>
      </div>
    </>
  );
}

function Cards() {
  return (
    <div className="skeleton-card-grid">
      {[0, 1, 2].map((index) => (
        <div className="card skeleton-panel skeleton-stack" key={index}>
          <Block height={112} className="skeleton-art" />
          <Block width="75%" height={18} />
          <Block width="50%" height={12} />
          <Block width="85%" height={10} />
          <div className="skeleton-row">
            <Block width="35%" height={24} />
            <Block width="45%" height={28} />
          </div>
        </div>
      ))}
    </div>
  );
}

function Detail() {
  return (
    <div className="card skeleton-panel skeleton-detail-body">
      <Block width={144} height={144} className="skeleton-art" />
      <div className="skeleton-stack">
        <Block width="30%" height={10} />
        <Block width="65%" height={24} />
        <Block width="90%" />
        <Block width="75%" />
        <div className="skeleton-row">
          <Block width={115} height={38} />
          <Block width={105} height={38} />
        </div>
      </div>
    </div>
  );
}

function Profile() {
  return (
    <div className="card profile-card skeleton-profile-body skeleton-stack">
      <Block width={76} height={76} className="skeleton-avatar" />
      <Block width="50%" height={20} />
      <Block width="70%" height={12} />
      <Block width={90} height={24} className="skeleton-pill" />
      <div className="skeleton-stack skeleton-profile-details">
        {[0, 1, 2].map((index) => (
          <div className="skeleton-row" key={index}>
            <Block width="30%" />
            <Block width="45%" />
          </div>
        ))}
      </div>
    </div>
  );
}

function Form() {
  return (
    <div className="card skeleton-panel form-grid">
      {[0, 1, 2, 3, 4, 5].map((index) => (
        <div className="skeleton-stack" key={index}>
          <Block width="40%" height={12} />
          <Block height={42} />
        </div>
      ))}
    </div>
  );
}

function Table() {
  return (
    <div className="card skeleton-panel skeleton-stack">
      {[0, 1, 2, 3, 4].map((index) => (
        <div className="skeleton-table-row" key={index}>
          <Block width="80%" height={index ? 14 : 10} />
          <Block width="70%" />
          <Block width="60%" height={22} />
        </div>
      ))}
    </div>
  );
}

function Messages() {
  return (
    <div className="skeleton-message-list">
      {[0, 1, 2, 3].map((index) => (
        <div
          className={`skeleton-stack skeleton-message ${index % 2 ? "outgoing" : ""}`}
          key={index}
        >
          <Block width="90%" />
          <Block width="65%" />
          <Block width="30%" height={8} />
        </div>
      ))}
    </div>
  );
}

export default function ResourceSkeleton({ variant = "list" }) {
  const content = {
    dashboard: <Dashboard />,
    cards: <Cards />,
    detail: <Detail />,
    profile: <Profile />,
    form: <Form />,
    table: <Table />,
    messages: <Messages />,
    stats: <Stats />,
    compact: <Rows compact />,
    list: <Rows />,
  };
  return (
    <div
      className={`loading skeleton-loading skeleton-${variant}`}
      role="status"
      aria-live="polite"
      aria-busy="true"
    >
      <span className="visually-hidden">Məlumatlar yüklənir…</span>
      <div aria-hidden="true">{content[variant] || content.list}</div>
    </div>
  );
}

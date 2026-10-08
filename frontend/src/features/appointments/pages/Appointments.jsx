import { WorkList } from "../../../components/common/work/WorkList.jsx";
import { appointmentsApi } from "../api/appointmentsApi.js";
import { appointmentTransitions } from "../utils/appointmentTransitions.js";
import { AppointmentEditor } from "../components/AppointmentEditor.jsx";
export function Appointments({ incoming }) {
  return (
    <WorkList
      type="appointments"
      incoming={incoming}
      resourceApi={appointmentsApi}
      transitions={appointmentTransitions}
      AppointmentEditor={AppointmentEditor}
    />
  );
}

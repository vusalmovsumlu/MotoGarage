import { useContext } from "react";

import { AppContext } from "../../app/providers/AppContext.js";

export const useApp = () => useContext(AppContext);

export interface Service {
  id: string;
  name: string;
  durationMinutes: number;
}

export interface ReservationSummary {
  id: string;
  name: string;
  phoneNumber: string;
}

export interface Slot {
  id: string;
  serviceId: string;
  startTime: string;
  serviceName: string;
  isBooked: boolean;
  reservation: ReservationSummary | null;
}

export interface Reservation {
  id: string;
  slotId: string;
  slotStartTime: string;
  serviceName: string;
  name: string;
  phoneNumber: string;
}

export interface ApiError {
  code: string;
  message: string;
}

const BASE_URL =
  (import.meta.env.VITE_API_URL as string | undefined) ?? "http://localhost:8080";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    headers: { "Content-Type": "application/json" },
    ...init,
  });

  if (response.status === 204) return undefined as T;

  const body = await response.json().catch(() => null);

  if (!response.ok) {
    const error = body as ApiError | null;
    throw new Error(error?.message ?? `Request failed (${response.status})`);
  }

  return body as T;
}

export const api = {
  listServices: () => request<Service[]>("/services"),

  listAvailableSlots: (serviceId: string, date: string) =>
    request<Slot[]>(
      `/slots/available?serviceId=${encodeURIComponent(serviceId)}&date=${encodeURIComponent(date)}`,
    ),

  createReservation: (input: { slotId: string; name: string; phoneNumber: string }) =>
    request<Reservation>("/reservations", {
      method: "POST",
      body: JSON.stringify(input),
    }),

  adminCreateService: (input: { name: string; durationMinutes: number }) =>
    request<Service>("/admin/services", {
      method: "POST",
      body: JSON.stringify(input),
    }),

  adminListServices: () => request<Service[]>("/admin/services"),

  adminCreateSlot: (input: { serviceId: string; startTime: string }) =>
    request<Slot>("/admin/slots", {
      method: "POST",
      body: JSON.stringify(input),
    }),

  adminListSlots: () => request<Slot[]>("/admin/slots"),

  adminDeleteSlot: (id: string) =>
    request<void>(`/admin/slots/${id}`, { method: "DELETE" }),

  adminListReservations: () => request<Reservation[]>("/admin/reservations"),

  adminCancelReservation: (id: string) =>
    request<void>(`/admin/reservations/${id}`, { method: "DELETE" }),
};

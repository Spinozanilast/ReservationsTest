import { createFileRoute } from "@tanstack/react-router";
import {
  Alert,
  Button,
  Chip,
  FieldError,
  Input,
  Label,
  ListBox,
  Modal,
  Select,
  Surface,
  Tabs,
  TextField,
} from "@heroui/react";
import { useForm } from "@tanstack/react-form";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState, type ReactNode } from "react";
import {
  BriefcaseIcon,
  CalendarBlankIcon,
  CalendarCheckIcon,
  CalendarSlashIcon,
  PackageIcon,
  PhoneIcon,
  PlusIcon,
  TimerIcon,
  TrashIcon,
  UserIcon,
  UsersIcon,
  XIcon,
} from "@phosphor-icons/react";
import { api, type Reservation, type Slot } from "~/lib/api";

const TAB_IDS = ["services", "slots", "reservations"] as const;
type TabId = (typeof TAB_IDS)[number];

export const Route = createFileRoute("/admin")({
  validateSearch: (search: Record<string, unknown>): { tab?: TabId } => ({
    tab:
      typeof search.tab === "string" && TAB_IDS.includes(search.tab as TabId)
        ? (search.tab as TabId)
        : undefined,
  }),
  component: AdminComponent,
});

function formatDateTime(value: string) {
  return new Date(value).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

function nowLocalInput() {
  const d = new Date();
  d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
  return d.toISOString().slice(0, 16);
}

function ErrorAlert({ error }: { error: Error | null }) {
  if (!error) return null;
  return (
    <Alert status="danger">
      <Alert.Indicator />
      <Alert.Content>
        <Alert.Title>{error.message}</Alert.Title>
      </Alert.Content>
    </Alert>
  );
}

function EmptyState({ icon, text }: { icon: ReactNode; text: string }) {
  return (
    <div className="flex flex-col items-center gap-2 px-5 py-10 text-center">
      <span className="text-muted">{icon}</span>
      <p className="text-sm text-muted">{text}</p>
    </div>
  );
}

function RowIcon({ children }: { children: ReactNode }) {
  return (
    <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-background-secondary text-accent">
      {children}
    </span>
  );
}

function AdminComponent() {
  const { tab } = Route.useSearch();
  const navigate = Route.useNavigate();
  const activeTab: TabId = tab ?? "services";

  useEffect(() => {
    if (!tab) {
      void navigate({ search: { tab: "services" }, replace: true });
    }
  }, [tab, navigate]);

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-3xl font-bold tracking-tight text-foreground">Admin</h1>
        <p className="text-sm text-muted">Manage services, slots and reservations.</p>
      </div>
      <Tabs
        selectedKey={activeTab}
        onSelectionChange={(key) => {
          const next = String(key) as TabId;
          void navigate({
            search: { tab: next },
            replace: true,
          });
        }}
      >
        <Tabs.ListContainer>
          <Tabs.List aria-label="Admin sections">
            <Tabs.Tab id="services" className="gap-2">
              <BriefcaseIcon />
              Services
              <Tabs.Indicator />
            </Tabs.Tab>
            <Tabs.Tab id="slots" className="gap-2">
              <CalendarBlankIcon />
              Slots
              <Tabs.Indicator />
            </Tabs.Tab>
            <Tabs.Tab id="reservations" className="gap-2">
              <UsersIcon />
              Reservations
              <Tabs.Indicator />
            </Tabs.Tab>
          </Tabs.List>
        </Tabs.ListContainer>
      </Tabs>
      {activeTab === "services" && <ServicesTab />}
      {activeTab === "slots" && <SlotsTab />}
      {activeTab === "reservations" && <ReservationsTab />}
    </div>
  );
}

function ServicesTab() {
  const queryClient = useQueryClient();
  const servicesQuery = useQuery({
    queryKey: ["services"],
    queryFn: api.adminListServices,
  });

  const createService = useMutation({
    mutationFn: api.adminCreateService,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["services"] }),
  });

  const form = useForm({
    defaultValues: { name: "", duration: "30" },
    validators: {
      onSubmit: ({ value }) => {
        if (!value.name.trim()) return "Enter a service name";
        if (Number(value.duration) <= 0) return "Duration must be greater than zero";
      },
    },
    onSubmit: async ({ value }) => {
      try {
        await createService.mutateAsync({
          name: value.name.trim(),
          durationMinutes: Number(value.duration),
        });
        form.reset();
      } catch {}
    },
  });

  const services = servicesQuery.data ?? [];

  return (
    <div className="flex flex-col gap-4">
      <ErrorAlert error={servicesQuery.error ?? createService.error} />
      <Surface className="p-5">
        <form
          onSubmit={(event) => {
            event.preventDefault();
            void form.handleSubmit();
          }}
        >
          <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
            <form.Field
              name="name"
              validators={{
                onChange: ({ value }) => (value.trim() ? undefined : "Name is required"),
              }}
            >
              {(field) => (
                <TextField
                  className="flex-1"
                  value={field.state.value}
                  onChange={(value) => field.handleChange(value)}
                  onBlur={field.handleBlur}
                  isInvalid={field.state.meta.errors.length > 0}
                >
                  <Label>Service name</Label>
                  <Input placeholder="Consultation" />
                  {field.state.meta.errors.length > 0 && (
                    <FieldError>{field.state.meta.errors.join(", ")}</FieldError>
                  )}
                </TextField>
              )}
            </form.Field>
            <form.Field
              name="duration"
              validators={{
                onChange: ({ value }) =>
                  Number(value) > 0 ? undefined : "Duration must be positive",
              }}
            >
              {(field) => (
                <TextField
                  type="number"
                  className="sm:w-48"
                  value={field.state.value}
                  onChange={(value) => field.handleChange(value)}
                  onBlur={field.handleBlur}
                  isInvalid={field.state.meta.errors.length > 0}
                >
                  <Label>Duration (min)</Label>
                  <Input />
                  {field.state.meta.errors.length > 0 && (
                    <FieldError>{field.state.meta.errors.join(", ")}</FieldError>
                  )}
                </TextField>
              )}
            </form.Field>
            <form.Subscribe
              selector={(state) => ({
                canSubmit: state.canSubmit,
                isSubmitting: state.isSubmitting,
              })}
            >
              {({ canSubmit, isSubmitting }) => (
                <Button type="submit" isDisabled={!canSubmit || isSubmitting}>
                  <PlusIcon weight="bold" />
                  Add
                </Button>
              )}
            </form.Subscribe>
          </div>
        </form>
      </Surface>
      <Surface className="divide-y divide-border">
        {services.length === 0 ? (
          <EmptyState icon={<PackageIcon className="size-6" />} text="No services yet." />
        ) : (
          services.map((service) => (
            <div key={service.id} className="flex items-center gap-4 px-5 py-3">
              <RowIcon>
                <BriefcaseIcon weight="fill" className="size-5" />
              </RowIcon>
              <div className="min-w-0 flex-1">
                <p className="truncate font-medium text-foreground">{service.name}</p>
              </div>
              <Chip color="default" variant="soft" size="sm">
                <TimerIcon className="size-3.5" />
                {service.durationMinutes} min
              </Chip>
            </div>
          ))
        )}
      </Surface>
    </div>
  );
}

function SlotsTab() {
  const queryClient = useQueryClient();
  const servicesQuery = useQuery({
    queryKey: ["services"],
    queryFn: api.adminListServices,
  });
  const slotsQuery = useQuery({ queryKey: ["slots"], queryFn: api.adminListSlots });

  const createSlot = useMutation({
    mutationFn: api.adminCreateSlot,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["slots"] }),
  });

  const deleteSlot = useMutation({
    mutationFn: api.adminDeleteSlot,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["slots"] }),
  });

  const services = servicesQuery.data ?? [];
  const slots = slotsQuery.data ?? [];

  const form = useForm({
    defaultValues: { serviceId: "", startTime: nowLocalInput() },
    validators: {
      onSubmit: ({ value }) =>
        value.serviceId ? undefined : "Select a service and start time",
    },
    onSubmit: async ({ value }) => {
      try {
        await createSlot.mutateAsync({
          serviceId: value.serviceId,
          startTime: new Date(value.startTime).toISOString(),
        });
        form.setFieldValue("startTime", nowLocalInput());
      } catch {}
    },
  });

  useEffect(() => {
    const first = servicesQuery.data?.[0];
    if (!form.store.state.values.serviceId && first) {
      form.setFieldValue("serviceId", first.id);
    }
  }, [servicesQuery.data, form]);

  return (
    <div className="flex flex-col gap-4">
      <ErrorAlert
        error={
          servicesQuery.error ?? slotsQuery.error ?? createSlot.error ?? deleteSlot.error
        }
      />
      <Surface className="p-5">
        <form
          onSubmit={(event) => {
            event.preventDefault();
            void form.handleSubmit();
          }}
        >
          <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
            <form.Field
              name="serviceId"
              validators={{
                onChange: ({ value }) => (value ? undefined : "Select a service"),
              }}
            >
              {(field) => (
                <Select
                  className="sm:flex-1"
                  fullWidth
                  placeholder="Select a service"
                  value={field.state.value || undefined}
                  onChange={(value) => {
                    field.handleChange(String(value ?? ""));
                  }}
                  isInvalid={field.state.meta.errors.length > 0}
                >
                  <Label>Service</Label>
                  <Select.Trigger>
                    <Select.Value />
                    <Select.Indicator />
                  </Select.Trigger>
                  <Select.Popover>
                    <ListBox>
                      {services.map((service) => (
                        <ListBox.Item
                          key={service.id}
                          id={service.id}
                          textValue={service.name}
                        >
                          {service.name}
                          <ListBox.ItemIndicator />
                        </ListBox.Item>
                      ))}
                    </ListBox>
                  </Select.Popover>
                </Select>
              )}
            </form.Field>
            <form.Field
              name="startTime"
              validators={{
                onChange: ({ value }) => (value ? undefined : "Pick a start time"),
              }}
            >
              {(field) => (
                <TextField
                  type="datetime-local"
                  className="sm:w-72"
                  value={field.state.value}
                  onChange={(value) => field.handleChange(value)}
                  onBlur={field.handleBlur}
                  isInvalid={field.state.meta.errors.length > 0}
                >
                  <Label>Start time</Label>
                  <Input />
                  {field.state.meta.errors.length > 0 && (
                    <FieldError>{field.state.meta.errors.join(", ")}</FieldError>
                  )}
                </TextField>
              )}
            </form.Field>
            <form.Subscribe
              selector={(state) => ({
                canSubmit: state.canSubmit,
                isSubmitting: state.isSubmitting,
              })}
            >
              {({ canSubmit, isSubmitting }) => (
                <Button type="submit" isDisabled={!canSubmit || isSubmitting}>
                  <PlusIcon weight="bold" />
                  Add
                </Button>
              )}
            </form.Subscribe>
          </div>
        </form>
      </Surface>
      <Surface className="divide-y divide-border">
        {slots.length === 0 ? (
          <EmptyState
            icon={<CalendarSlashIcon className="size-6" />}
            text="No slots yet."
          />
        ) : (
          slots.map((slot) => (
            <div key={slot.id} className="flex items-center gap-4 px-5 py-3">
              <RowIcon>
                <CalendarCheckIcon weight="fill" className="size-5" />
              </RowIcon>
              <div className="min-w-0 flex-1">
                <p className="truncate font-medium text-foreground">{slot.serviceName}</p>
                <p className="text-sm text-muted">
                  {formatDateTime(slot.startTime)}
                  {slot.isBooked && slot.reservation
                    ? ` · Booked by ${slot.reservation.name}`
                    : ""}
                </p>
              </div>
              {slot.isBooked ? (
                <Chip color="warning" variant="soft" size="sm">
                  Booked
                </Chip>
              ) : (
                <Chip color="success" variant="soft" size="sm">
                  Available
                </Chip>
              )}
              <DeleteSlotButton
                slot={slot}
                isDeleting={deleteSlot.isPending && deleteSlot.variables === slot.id}
                onDelete={(id) => deleteSlot.mutate(id)}
              />
            </div>
          ))
        )}
      </Surface>
    </div>
  );
}

function DeleteSlotButton({
  slot,
  isDeleting,
  onDelete,
}: {
  slot: Slot;
  isDeleting: boolean;
  onDelete: (id: string) => void;
}) {
  const [open, setOpen] = useState(false);
  return (
    <>
      <Button
        isIconOnly
        variant="ghost"
        aria-label="Delete slot"
        className="text-muted"
        onPress={() => setOpen(true)}
      >
        <TrashIcon className="size-4" />
      </Button>
      <Modal.Backdrop isOpen={open} onOpenChange={setOpen}>
        <Modal.Container>
          <Modal.Dialog className="sm:max-w-xs">
            <Modal.Header>
              <Modal.Heading>Delete slot</Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <p className="text-sm text-muted">
                Delete {slot.serviceName} on {formatDateTime(slot.startTime)}?
              </p>
            </Modal.Body>
            <Modal.Footer>
              <Button variant="secondary" slot="close">
                Cancel
              </Button>
              <Button
                variant="danger"
                isDisabled={isDeleting}
                onPress={() => {
                  setOpen(false);
                  onDelete(slot.id);
                }}
              >
                <TrashIcon weight="bold" />
                Delete
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </>
  );
}

function ReservationsTab() {
  const queryClient = useQueryClient();
  const reservationsQuery = useQuery({
    queryKey: ["reservations"],
    queryFn: api.adminListReservations,
  });

  const cancelReservation = useMutation({
    mutationFn: api.adminCancelReservation,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reservations"] });
      queryClient.invalidateQueries({ queryKey: ["slots"] });
    },
  });

  const reservations = reservationsQuery.data ?? [];

  return (
    <div className="flex flex-col gap-4">
      <ErrorAlert error={reservationsQuery.error ?? cancelReservation.error} />
      <Surface className="divide-y divide-border">
        {reservations.length === 0 ? (
          <EmptyState
            icon={<UsersIcon className="size-6" />}
            text="No reservations yet."
          />
        ) : (
          reservations.map((reservation) => (
            <div key={reservation.id} className="flex items-center gap-4 px-5 py-3">
              <RowIcon>
                <UserIcon weight="fill" className="size-5" />
              </RowIcon>
              <div className="min-w-0 flex-1">
                <p className="truncate font-medium text-foreground">
                  {reservation.serviceName}
                </p>
                <p className="flex flex-wrap items-center gap-x-3 gap-y-0.5 text-sm text-muted">
                  <span className="inline-flex items-center gap-1">
                    <CalendarBlankIcon className="size-3.5" />
                    {formatDateTime(reservation.slotStartTime)}
                  </span>
                  <span className="inline-flex items-center gap-1">
                    <PhoneIcon className="size-3.5" />
                    {reservation.name} · {reservation.phoneNumber}
                  </span>
                </p>
              </div>
              <CancelReservationButton
                reservation={reservation}
                isCancelling={
                  cancelReservation.isPending &&
                  cancelReservation.variables === reservation.id
                }
                onCancel={(id) => cancelReservation.mutate(id)}
              />
            </div>
          ))
        )}
      </Surface>
    </div>
  );
}

function CancelReservationButton({
  reservation,
  isCancelling,
  onCancel,
}: {
  reservation: Reservation;
  isCancelling: boolean;
  onCancel: (id: string) => void;
}) {
  const [open, setOpen] = useState(false);
  return (
    <>
      <Button size="sm" variant="danger-soft" onPress={() => setOpen(true)}>
        <XIcon weight="bold" />
        Cancel
      </Button>
      <Modal.Backdrop isOpen={open} onOpenChange={setOpen}>
        <Modal.Container>
          <Modal.Dialog className="sm:max-w-xs">
            <Modal.Header>
              <Modal.Heading>Cancel reservation</Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <p className="text-sm text-muted">
                Cancel {reservation.name} — {reservation.serviceName} on{" "}
                {formatDateTime(reservation.slotStartTime)}? The slot will become
                available again.
              </p>
            </Modal.Body>
            <Modal.Footer>
              <Button variant="secondary" slot="close">
                Keep
              </Button>
              <Button
                variant="danger"
                isDisabled={isCancelling}
                onPress={() => {
                  setOpen(false);
                  onCancel(reservation.id);
                }}
              >
                <XIcon weight="bold" />
                Cancel reservation
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </>
  );
}

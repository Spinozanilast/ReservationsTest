import { createFileRoute } from "@tanstack/react-router";
import {
  Alert,
  Button,
  Chip,
  CloseButton,
  Description,
  FieldError,
  Input,
  Label,
  ListBox,
  Modal,
  Select,
  Surface,
  TextField,
} from "@heroui/react";
import { useForm } from "@tanstack/react-form";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { usePhoneInput } from "react-international-phone";
import { useState } from "react";
import {
  CalendarBlankIcon,
  CalendarXIcon,
  CheckIcon,
  CircleNotchIcon,
} from "@phosphor-icons/react";
import { api, type Service, type Slot } from "~/lib/api";

export const Route = createFileRoute("/client")({
  component: RouteComponent,
});

function todayString() {
  return new Date().toISOString().slice(0, 10);
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

function ServiceSelect({
  services,
  value,
  onChange,
  className,
}: {
  services: Service[];
  value: string;
  onChange: (value: string) => void;
  className?: string;
}) {
  return (
    <Select
      className={className}
      fullWidth
      placeholder="Select a service"
      value={value}
      onChange={(next) => onChange(next ? String(next) : "")}
    >
      <Label>Service</Label>
      <Select.Trigger>
        <Select.Value />
        <Select.Indicator />
      </Select.Trigger>
      <Select.Popover>
        <ListBox>
          {services.map((service) => (
            <ListBox.Item key={service.id} id={service.id} textValue={service.name}>
              {service.name} ({service.durationMinutes} min)
              <ListBox.ItemIndicator />
            </ListBox.Item>
          ))}
        </ListBox>
      </Select.Popover>
    </Select>
  );
}

function RouteComponent() {
  const [serviceId, setServiceId] = useState("");
  const [date, setDate] = useState(todayString());
  const [selectedSlot, setSelectedSlot] = useState<Slot | null>(null);
  const [success, setSuccess] = useState("");

  const servicesQuery = useQuery({ queryKey: ["services"], queryFn: api.listServices });
  const services = servicesQuery.data ?? [];

  const activeServiceId = serviceId || services[0]?.id || "";

  const slotsQuery = useQuery({
    queryKey: ["slots", "available", activeServiceId, date],
    queryFn: () => api.listAvailableSlots(activeServiceId, date),
    enabled: activeServiceId !== "" && date !== "",
    placeholderData: (previous) => previous,
  });
  const slots = slotsQuery.data ?? [];

  const loading = servicesQuery.isPending || slotsQuery.isFetching;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-3xl font-bold tracking-tight text-foreground">Book a slot</h1>
        <p className="text-sm text-muted">
          Pick a service and a date, then reserve your spot.
        </p>
      </div>

      {(servicesQuery.error ?? slotsQuery.error) && (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>
              {(slotsQuery.error ?? servicesQuery.error)?.message}
            </Alert.Title>
          </Alert.Content>
        </Alert>
      )}

      {success && (
        <Alert status="success">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{success}</Alert.Title>
          </Alert.Content>
          <CloseButton onPress={() => setSuccess("")} />
        </Alert>
      )}

      <Surface className="flex flex-col gap-4 p-5 sm:flex-row">
        <ServiceSelect
          services={services}
          value={activeServiceId}
          onChange={setServiceId}
          className="sm:flex-1"
        />
        <TextField type="date" value={date} onChange={setDate} className="sm:w-56">
          <Label>Date</Label>
          <Input />
        </TextField>
      </Surface>

      <h2 className="text-xl font-semibold text-foreground">
        {loading ? (
          <span className="inline-flex items-center gap-2">
            <CircleNotchIcon className="animate-spin" />
            Loading slots…
          </span>
        ) : (
          <span className="inline-flex items-baseline gap-2">
            {slots.length} slot{slots.length === 1 ? "" : "s"} available
            {services.length > 0 && (
              <span className="text-sm font-normal text-muted">
                for{" "}
                {services.find((service) => service.id === activeServiceId)?.name ??
                  "this service"}
              </span>
            )}
          </span>
        )}
      </h2>

      <Surface>
        {!loading && slots.length === 0 ? (
          <div className="flex flex-col items-center gap-2 px-5 py-10 text-center">
            <span className="text-muted">
              <CalendarXIcon className="size-6" />
            </span>
            <p className="text-sm text-muted">
              No slots available for this service and date.
            </p>
          </div>
        ) : (
          <ListBox
            aria-label="Available slots"
            className="p-2"
            selectionMode="none"
            onAction={(key) => {
              const slot = slots.find((item) => item.id === key);
              if (slot) setSelectedSlot(slot);
            }}
          >
            {slots.map((slot) => (
              <ListBox.Item
                key={slot.id}
                id={slot.id}
                textValue={formatDateTime(slot.startTime)}
                isDisabled={slot.isBooked}
              >
                <div className="flex items-center gap-3">
                  <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-background-secondary text-accent">
                    <CalendarBlankIcon weight="fill" className="size-4.5" />
                  </span>
                  <div className="flex flex-1 flex-col gap-0.5">
                    <Label>{formatDateTime(slot.startTime)}</Label>
                    <Description>
                      {slot.isBooked ? "Already booked" : "Click to book"}
                    </Description>
                  </div>
                  {slot.isBooked ? (
                    <Chip color="warning" variant="soft" size="sm">
                      Booked
                    </Chip>
                  ) : null}
                </div>
              </ListBox.Item>
            ))}
          </ListBox>
        )}
      </Surface>

      <Modal.Backdrop
        isOpen={selectedSlot !== null}
        onOpenChange={(open) => {
          if (!open) setSelectedSlot(null);
        }}
      >
        <Modal.Container>
          <Modal.Dialog className="sm:max-w-md">
            <Modal.CloseTrigger />
            {selectedSlot && (
              <BookingForm
                key={selectedSlot.id}
                slot={selectedSlot}
                onClose={() => setSelectedSlot(null)}
                onBooked={(bookedSlot) => {
                  setSuccess(
                    `Reserved ${bookedSlot.serviceName} at ${formatDateTime(bookedSlot.startTime)}.`,
                  );
                  setSelectedSlot(null);
                }}
              />
            )}
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </div>
  );
}

function validatePhone(value: string) {
  const digits = value.replace(/\D/g, "");
  if (!digits) return "Phone number is required";
  if (!value.startsWith("+")) return 'Phone number must start with "+"';
  if (digits.length < 7)
    return `Enter at least 7 digits after the country code (${digits.length} entered)`;
  if (digits.length > 15)
    return `Max 15 digits after the country code (${digits.length} entered)`;
  return undefined;
}

function PhoneField({
  value,
  onChange,
  onBlur,
  errors,
}: {
  value: string;
  onChange: (value: string) => void;
  onBlur: (event?: unknown) => void;
  errors: readonly unknown[];
}) {
  // Handles formatting, auto-inserts "+" + dial code and caps input length
  // by the country mask.
  const phoneInput = usePhoneInput({
    defaultCountry: "by",
    value,
    disableDialCodePrefill: true,
    onChange: ({ phone }) => onChange(phone),
  });

  return (
    <TextField
      isInvalid={errors.length > 0}
      className="w-full"
    >
      <Label>Phone number</Label>
      <Input
        ref={phoneInput.inputRef}
        value={phoneInput.inputValue}
        onChange={phoneInput.handlePhoneValueChange}
        onBlur={onBlur}
        type="tel"
        autoComplete="tel"
        placeholder="+375 29 123-45-67"
      />
      {errors.length > 0 ? (
        <FieldError>{errors.join(", ")}</FieldError>
      ) : (
        <Description>Formatted automatically with a country code</Description>
      )}
    </TextField>
  );
}

function BookingForm({
  slot,
  onClose,
  onBooked,
}: {
  slot: Slot;
  onClose: () => void;
  onBooked: (bookedSlot: Slot) => void;
}) {
  const queryClient = useQueryClient();

  const createReservation = useMutation({
    mutationFn: api.createReservation,
    onSuccess: (reservation) => {
      queryClient.invalidateQueries({ queryKey: ["slots"] });
      onBooked({
        ...slot,
        isBooked: true,
        reservation: {
          id: reservation.id,
          name: reservation.name,
          phoneNumber: reservation.phoneNumber,
        },
      });
    },
  });

  const form = useForm({
    defaultValues: { name: "", phone: "" },
    onSubmit: async ({ value }) => {
      try {
        await createReservation.mutateAsync({
          slotId: slot.id,
          name: value.name.trim(),
          phoneNumber: value.phone.trim(),
        });
      } catch {}
    },
  });

  return (
    <>
      <Modal.Header>
        <Modal.Heading>Book {slot.serviceName}</Modal.Heading>
        <p className="text-sm text-muted">{formatDateTime(slot.startTime)}</p>
      </Modal.Header>
      <Modal.Body>
        <form
          id="booking-form"
          className="flex flex-col gap-4"
          onSubmit={(event) => {
            event.preventDefault();
            void form.handleSubmit();
          }}
        >
          {createReservation.isError && (
            <Alert status="danger">
              <Alert.Indicator />
              <Alert.Content>
                <Alert.Title>{createReservation.error.message}</Alert.Title>
              </Alert.Content>
            </Alert>
          )}
          <form.Field
            name="name"
            validators={{
              onChange: ({ value }) => (value.trim() ? undefined : "Name is required"),
            }}
          >
            {(field) => (
              <TextField
                autoFocus
                value={field.state.value}
                onChange={(value) => field.handleChange(value)}
                onBlur={field.handleBlur}
                isInvalid={field.state.meta.errors.length > 0}
              >
                <Label>Your name</Label>
                <Input placeholder="Jane Doe" />
                {field.state.meta.errors.length > 0 && (
                  <FieldError>{field.state.meta.errors.join(", ")}</FieldError>
                )}
              </TextField>
            )}
          </form.Field>
          <form.Field
            name="phone"
            validators={{ onChange: ({ value }) => validatePhone(value) }}
          >
            {(field) => (
              <PhoneField
                value={field.state.value}
                onChange={(value) => field.handleChange(value)}
                onBlur={field.handleBlur}
                errors={field.state.meta.errors}
              />
            )}
          </form.Field>
        </form>
      </Modal.Body>
      <Modal.Footer>
        <Button variant="secondary" onPress={onClose}>
          Cancel
        </Button>
        <form.Subscribe
          selector={(state) => ({
            isSubmitting: state.isSubmitting,
            canSubmit: state.canSubmit,
          })}
        >
          {({ canSubmit, isSubmitting }) => (
            <Button
              form="booking-form"
              type="submit"
              isDisabled={!canSubmit || isSubmitting}
            >
              <CheckIcon weight="bold" />
              Confirm
            </Button>
          )}
        </form.Subscribe>
      </Modal.Footer>
    </>
  );
}

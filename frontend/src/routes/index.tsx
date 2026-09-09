import { buttonVariants } from "@heroui/react";
import { createFileRoute } from "@tanstack/react-router";
import { BusIcon, CalendarCheckIcon, GearSixIcon } from "@phosphor-icons/react";
import { CustomLink } from "~/components/CustomLink";

export const Route = createFileRoute("/")({
  component: RouteComponent,
});

function RouteComponent() {
  return (
    <div className="flex min-h-full flex-col items-center justify-center p-4">
      <div className="w-full max-w-md rounded-3xl border border-border bg-background-secondary p-8 text-center">
        <span className="mx-auto flex size-14 items-center justify-center rounded-2xl bg-accent/15 text-accent">
          <BusIcon weight="fill" className="size-7" />
        </span>
        <h1 className="mt-4 text-2xl font-bold text-balance text-foreground">
          Welcome to the Reservations Module
        </h1>
        <p className="mt-1 text-sm text-muted">
          Book an appointment or manage your services.
        </p>
        <div className="mt-6 flex flex-col gap-3">
          <CustomLink
            className={buttonVariants({
              variant: "primary",
              size: "md",
              fullWidth: true,
            })}
            to="/admin"
            search={{ tab: "services" }}
          >
            <GearSixIcon weight="bold" className="mr-1" />
            Admin Panel
          </CustomLink>
          <CustomLink
            className={buttonVariants({
              variant: "secondary",
              size: "lg",
              fullWidth: true,
            })}
            to="/client"
          >
            <CalendarCheckIcon weight="bold" className="mr-1" />
            Client Panel
          </CustomLink>
        </div>
      </div>
    </div>
  );
}

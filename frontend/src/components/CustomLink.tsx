import React from "react";
import { createLink } from "@tanstack/react-router";
import { linkVariants } from "@heroui/styles";
import type { LinkVariants } from "@heroui/styles";
import type { LinkComponent } from "@tanstack/react-router";
import { Link } from "@heroui/react";

interface HeroUILinkProps extends LinkVariants {
  className?: string;
}

const HeroUILinkComponent = React.forwardRef<HTMLAnchorElement, HeroUILinkProps>(
  ({ className, ...props }, ref) => (
    <Link ref={ref} className={linkVariants({ className }).base()} {...props} />
  ),
);

const CreatedLinkComponent = createLink(HeroUILinkComponent);

export const CustomLink: LinkComponent<typeof HeroUILinkComponent> = (props) => {
  return <CreatedLinkComponent preload={"intent"} {...props} />;
};

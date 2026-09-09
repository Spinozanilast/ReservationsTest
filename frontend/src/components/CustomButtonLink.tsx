import React from 'react'
import { createLink } from '@tanstack/react-router'
import { buttonVariants } from '@heroui/styles'
import type { ButtonVariants } from '@heroui/styles'
import type { LinkComponent } from '@tanstack/react-router'

interface HeroUIButtonLinkProps extends ButtonVariants {
  className?: string
}

const HeroUIButtonLinkComponent = React.forwardRef<
  HTMLAnchorElement,
  HeroUIButtonLinkProps
>(({ className, variant, size, fullWidth, isIconOnly, ...props }, ref) => (
  <a
    ref={ref}
    className={buttonVariants({ className, variant, size, fullWidth, isIconOnly })}
    {...props}
  />
))

const CreatedButtonLinkComponent = createLink(HeroUIButtonLinkComponent)

export const CustomButtonLink: LinkComponent<
  typeof HeroUIButtonLinkComponent
> = (props) => {
  return <CreatedButtonLinkComponent preload={'intent'} {...props} />
}
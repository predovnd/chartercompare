import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from './ui/accordion';

export function Faq() {
  return (
    <section id="faq" className="container py-16 md:py-24">
      <div className="max-w-3xl mx-auto">
        <div className="text-center mb-12">
          <h2 className="text-3xl md:text-4xl font-bold mb-4">Frequently asked questions</h2>
        </div>
        <Accordion type="single" collapsible className="w-full">
          <AccordionItem value="item-1">
            <AccordionTrigger>How long does it take to get quotes?</AccordionTrigger>
            <AccordionContent>
              We aim to send you a comparison of quotes within 24 hours of receiving your request. During peak periods, it may take up to 48 hours.
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2">
            <AccordionTrigger>Are the operators licensed and insured?</AccordionTrigger>
            <AccordionContent>
              Yes, all operators in our network are fully licensed and carry comprehensive insurance. We verify credentials before adding operators to our platform.
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-3">
            <AccordionTrigger>Is there a fee for using Charter Compare?</AccordionTrigger>
            <AccordionContent>
              No, our comparison service is completely free. There's no obligation to book, and we don't charge any fees for providing quotes.
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-4">
            <AccordionTrigger>What areas do you cover?</AccordionTrigger>
            <AccordionContent>
              We work with operators across Australia, covering all major cities and regional areas. If you're planning a trip, we can help find options in your area.
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-5">
            <AccordionTrigger>Can I book multi-day trips?</AccordionTrigger>
            <AccordionContent>
              Currently, we focus on single-day trips to ensure the best service and pricing. For multi-day bookings, we recommend contacting operators directly.
            </AccordionContent>
          </AccordionItem>
        </Accordion>
      </div>
    </section>
  );
}

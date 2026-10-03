import { Component } from '@angular/core';
@Component({
  selector: 'app-root',
  standalone: true,
  template: `<main><p>SMARTPAY • DEMO ENVIRONMENT</p><h1>Digital payments, built transparently.</h1><p>Learning project for simulated SAR wallet payments. No real money or payment provider is connected.</p><section><h2>Implementation status</h2><p>Backend API scaffolding is available. Frontend API integration is planned.</p></section></main>`,
  styles: [`main{font-family:Arial,sans-serif;max-width:800px;margin:72px auto;padding:24px;color:#17212b}h1{font-size:48px}section{border:1px solid #dbe2e8;border-radius:16px;padding:24px}`]
})
export class AppComponent {}

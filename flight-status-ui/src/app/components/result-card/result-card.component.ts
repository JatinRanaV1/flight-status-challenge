import { Component, Input } from '@angular/core';
import { DatePipe, NgIf } from '@angular/common';
import { FlightStatusResult } from '../../models/flight-status.model';

@Component({
  selector: 'app-result-card',
  standalone: true,
  imports: [NgIf, DatePipe],
  templateUrl: './result-card.component.html',
  styleUrl: './result-card.component.css'
})
export class ResultCardComponent {
  @Input() result:       FlightStatusResult | null = null;
  @Input() errorMessage: string | null = null;

  get statusClass(): string {
    switch (this.result?.status) {
      case 'OnTime':    return 'status-on-time';
      case 'Delayed':   return 'status-delayed';
      case 'Cancelled': return 'status-cancelled';
      case 'Diverted':  return 'status-diverted';
      default:          return 'status-unknown';
    }
  }
}

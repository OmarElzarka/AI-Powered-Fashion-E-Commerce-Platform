import { Pipe, PipeTransform } from '@angular/core';
import { environment } from '../../../environments/environment';

@Pipe({
  name: 'backendImage',
  standalone: true
})
export class BackendImagePipe implements PipeTransform {

  transform(value: string | undefined): string {
    if (!value) return '';
    if (value.startsWith('http')) return value;
    
    const baseUrl = environment.baseUrl.replace('api/', '');
    return baseUrl + value.replace(/^\//, '');
  }

}
